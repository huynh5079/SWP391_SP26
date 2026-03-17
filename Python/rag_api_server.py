import json
import os
import re
import asyncio
from contextlib import asynccontextmanager
from dataclasses import dataclass
from decimal import Decimal
from datetime import datetime
from pathlib import Path
from typing import Any, Optional, AsyncGenerator
import time
from uuid import uuid4

import faiss
import json5
import numpy as np
import pyodbc
from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from groq import Groq
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer

from db_service import DatabaseService

load_dotenv()


@dataclass
class Chunk:
    text: str
    meta: dict[str, Any]


@dataclass
class ConversationMessage:
    role: str
    content: str
    timestamp: str = None

    def __post_init__(self):
        if self.timestamp is None:
            self.timestamp = datetime.now().isoformat()


class AskRequest(BaseModel):
    question: str = Field(..., min_length=2)
    top_k: int = Field(default=5, ge=1, le=20)
    role: str = Field(default="user")
    session_id: Optional[str] = Field(default=None, description="Session ID for conversation tracking")
    user_id: Optional[str] = Field(default=None, description="User ID to save conversation to database")
    user_profile_context: Optional[str] = Field(default=None, description="Requester profile context for personalization")
    save_to_db: bool = Field(default=True, description="Whether to save conversation to database")
    

class ChatSessionResponse(BaseModel):
    session_id: str
    message: str
    sources: list[dict[str, Any]]
    error: Optional[str] = None


class RagEngine:
    def __init__(self) -> None:
        self.embedding_model_name = os.getenv(
            "RAG_EMBEDDING_MODEL", "sentence-transformers/all-MiniLM-L6-v2"
        )
        self.llm_model_name = os.getenv("RAG_LLM_MODEL", "llama-3.3-70b-versatile")
        self.data_limit = int(os.getenv("RAG_DATA_LIMIT", "1200"))
        self.default_top_k = int(os.getenv("RAG_TOP_K_DEFAULT", "3"))  # Default: 3 for concise answers
        self.auto_reload_interval = int(os.getenv("RAG_AUTO_RELOAD_SECONDS", "60"))  # Default: 60 seconds

        kb_dir = Path(os.getenv("RAG_KB_DIR", "./kb"))
        self.kb_dir = kb_dir
        self.kb_dir.mkdir(parents=True, exist_ok=True)

        self.index_path = self.kb_dir / "faiss.index"
        self.chunks_path = self.kb_dir / "chunks.json"
        self.meta_path = self.kb_dir / "meta.json"

        self._embedding_model = SentenceTransformer(self.embedding_model_name)

        api_key = os.getenv("GROQ_API_KEY", "").strip()
        self._llm_api_key_set = bool(api_key)
        self._llm = Groq(api_key=api_key) if api_key else None
        self._llm_last_error: str | None = None
        self._llm_last_error_type: str | None = None
        self._llm_last_error_at: str | None = None
        self._llm_blocked_until: float = 0.0
        self._llm_cooldown_seconds = int(os.getenv("RAG_LLM_COOLDOWN_SECONDS", "180"))

        if not self._llm_api_key_set:
            print("[RAG] GROQ_API_KEY is missing. System will run in data fallback mode.")

        self._chunks: list[Chunk] = []
        self._index: faiss.IndexFlatIP | None = None
        self._last_reload: str | None = None
        
        # Conversation sessions: session_id -> list of ConversationMessage
        self._sessions: dict[str, list[ConversationMessage]] = {}
        
        # Background task control
        self._auto_reload_task: Optional[asyncio.Task] = None
        self._stop_auto_reload = False
        
        # Database service for saving conversations
        try:
            self.db_service = DatabaseService(self.db_connection_string)
            print(f"[RAG] Database service initialized")
        except Exception as e:
            print(f"[RAG] Database service initialization failed: {e}")
            self.db_service = None

    def _categorize_llm_error(self, error: Exception) -> tuple[str, str]:
        raw = str(error)
        lowered = raw.lower()

        if "403" in lowered and "access denied" in lowered:
            return (
                "access_denied_network",
                "Groq từ chối truy cập từ mạng hiện tại (403 Access denied).",
            )
        if "401" in lowered or "invalid api key" in lowered or "authentication" in lowered:
            return (
                "auth",
                "GROQ_API_KEY không hợp lệ hoặc đã hết hiệu lực.",
            )
        if "model" in lowered and ("not found" in lowered or "does not exist" in lowered):
            return (
                "model",
                f"Model '{self.llm_model_name}' không khả dụng cho API key hiện tại.",
            )
        if "timed out" in lowered or "timeout" in lowered or "name resolution" in lowered:
            return (
                "network",
                "Không thể kết nối đến Groq do timeout hoặc DNS/network.",
            )
        if not self._llm_api_key_set:
            return (
                "missing_key",
                "Thiếu GROQ_API_KEY nên không thể dùng LLM bên ngoài.",
            )
        return (
            "unknown",
            f"Lỗi khi gọi Groq: {raw}",
        )

    def _record_llm_error(self, error: Exception) -> None:
        error_type, diagnosis = self._categorize_llm_error(error)
        self._llm_last_error_type = error_type
        self._llm_last_error = diagnosis
        self._llm_last_error_at = datetime.now().isoformat()
        self._llm_blocked_until = time.time() + self._llm_cooldown_seconds

    def _clear_llm_error(self) -> None:
        self._llm_last_error = None
        self._llm_last_error_type = None
        self._llm_last_error_at = None
        self._llm_blocked_until = 0.0

    def _can_call_llm(self) -> tuple[bool, str | None]:
        if self._llm is None:
            return False, "GROQ_API_KEY chưa được cấu hình."

        now = time.time()
        if now < self._llm_blocked_until:
            wait_seconds = int(self._llm_blocked_until - now)
            return (
                False,
                f"LLM đang tạm thời bị chặn sau lỗi gần nhất. Thử lại sau ~{wait_seconds}s.",
            )

        return True, None

    def probe_llm_health(self) -> dict[str, Any]:
        can_call, reason = self._can_call_llm()
        if not can_call:
            return {
                "enabled": self._llm is not None,
                "ok": False,
                "model": self.llm_model_name,
                "cooldown_seconds": self._llm_cooldown_seconds,
                "last_error_type": self._llm_last_error_type,
                "last_error": self._llm_last_error or reason,
                "last_error_at": self._llm_last_error_at,
            }

        try:
            # Lightweight probe to verify key/network accessibility.
            self._llm.models.list()
            self._clear_llm_error()
            return {
                "enabled": True,
                "ok": True,
                "model": self.llm_model_name,
                "cooldown_seconds": self._llm_cooldown_seconds,
                "last_error_type": None,
                "last_error": None,
                "last_error_at": None,
            }
        except Exception as ex:
            self._record_llm_error(ex)
            return {
                "enabled": True,
                "ok": False,
                "model": self.llm_model_name,
                "cooldown_seconds": self._llm_cooldown_seconds,
                "last_error_type": self._llm_last_error_type,
                "last_error": self._llm_last_error,
                "last_error_at": self._llm_last_error_at,
            }

    @property
    def db_connection_string(self) -> str:
        from_env = os.getenv("AEMS_DB_CONNECTION_STRING", "").strip()
        if from_env:
            return from_env

        appsettings_path = Path(__file__).resolve().parent.parent / "AEMS_Solution" / "appsettings.Development.json"
        if appsettings_path.exists():
            with appsettings_path.open("r", encoding="utf-8") as f:
                raw_content = f.read()
            data = self._parse_jsonc(raw_content)
            conn = data.get("ConnectionStrings", {}).get("DefaultConnection", "").strip()
            if conn:
                return conn

        raise ValueError(
            "Không tìm thấy connection string. Hãy set AEMS_DB_CONNECTION_STRING hoặc appsettings.Development.json"
        )

    @staticmethod
    def _parse_jsonc(content: str) -> dict[str, Any]:
        return json5.loads(content)

    @staticmethod
    def _to_pyodbc_conn_str(dotnet_conn: str) -> str:
        parts: dict[str, str] = {}
        for token in dotnet_conn.split(";"):
            token = token.strip()
            if not token or "=" not in token:
                continue
            key, value = token.split("=", 1)
            parts[key.strip().lower()] = value.strip()

        server = parts.get("server") or parts.get("data source")
        database = parts.get("initial catalog") or parts.get("database")
        user = parts.get("user id") or parts.get("uid")
        password = parts.get("password") or parts.get("pwd")
        timeout = parts.get("connection timeout", "30")

        if not server or not database or not user or not password:
            raise ValueError("Connection string không đủ thông tin Server/Database/User/Password")

        server = re.sub(r"^tcp:", "", server, flags=re.IGNORECASE)

        drivers = [
            "ODBC Driver 18 for SQL Server",
            "ODBC Driver 17 for SQL Server",
            "SQL Server",
        ]

        selected_driver = None
        installed = {d.strip() for d in pyodbc.drivers()}
        for d in drivers:
            if d in installed:
                selected_driver = d
                break

        if not selected_driver:
            raise ValueError(
                "Không tìm thấy ODBC SQL Server Driver. Hãy cài ODBC Driver 18 hoặc 17 for SQL Server."
            )

        encrypt = RagEngine._to_odbc_yes_no(parts.get("encrypt", "True"))
        trust = RagEngine._to_odbc_yes_no(parts.get("trustservercertificate", "False"))

        return (
            f"DRIVER={{{selected_driver}}};"
            f"SERVER={server};"
            f"DATABASE={database};"
            f"UID={user};"
            f"PWD={password};"
            f"Encrypt={encrypt};"
            f"TrustServerCertificate={trust};"
            f"Connection Timeout={timeout};"
        )

    @staticmethod
    def _to_odbc_yes_no(value: Any) -> str:
        normalized = str(value).strip().lower()
        if normalized in {"true", "yes", "1", "mandatory", "strict"}:
            return "yes"
        if normalized in {"false", "no", "0", "optional"}:
            return "no"
        return str(value)

    def _connect(self) -> pyodbc.Connection:
        conn_str = self._to_pyodbc_conn_str(self.db_connection_string)
        return pyodbc.connect(conn_str)

    def _fetch_rows(self, query: str) -> list[dict[str, Any]]:
        with self._connect() as conn:
            with conn.cursor() as cursor:
                cursor.execute(query)
                columns = [desc[0] for desc in cursor.description]
                rows = cursor.fetchall()
        return [dict(zip(columns, row)) for row in rows]

    def _fetch_rows_with_fallback(
        self,
        primary_query: str,
        fallback_query: str,
        query_name: str,
    ) -> list[dict[str, Any]]:
        try:
            return self._fetch_rows(primary_query)
        except Exception as ex:
            print(f"[RAG] Primary query failed ({query_name}): {ex}")
            print(f"[RAG] Falling back to simplified query for: {query_name}")
            return self._fetch_rows(fallback_query)
    def _build_chunks_from_db(self) -> list[Chunk]:
        top_n = self.data_limit

        # 1. Event data with full details
        event_rows = self._fetch_rows_with_fallback(
            primary_query=f"""
            SELECT TOP ({top_n})
                e.Id AS EventId,
                e.Title,
                e.Description,
                e.Status,
                e.Type,
                e.Mode,
                e.StartTime,
                e.EndTime,
                e.MaxCapacity,
                e.MeetingUrl,
                d.Name AS DepartmentName,
                sem.Name AS SemesterName,
                loc.Name AS LocationName,
                organizerUser.FullName AS OrganizerName,
                t.Name AS TopicName,
                e.IsDepositRequired,
                e.DepositAmount,
                e.PublishedAt,
                (SELECT COUNT(*) FROM [EventWaitlist] WHERE EventId = e.Id) AS WaitlistCount,
                (SELECT COUNT(*) FROM [EventTeam] WHERE EventId = e.Id) AS TeamMemberCount,
                (SELECT COUNT(*) FROM [EventAgenda] WHERE EventId = e.Id) AS AgendaCount,
                (SELECT COUNT(*) FROM [Ticket] WHERE EventId = e.Id) AS RegisteredCount,
                (SELECT AVG(CAST(Rating AS FLOAT)) FROM [Feedback] WHERE EventId = e.Id) AS AvgRating,
                (SELECT COUNT(*) FROM [Feedback] WHERE EventId = e.Id) AS FeedbackCount,
                e.CreatedAt,
                e.UpdatedAt
            FROM [Event] e
            LEFT JOIN [Department] d ON d.Id = e.DepartmentId
            LEFT JOIN [Semester] sem ON sem.Id = e.SemesterId
            LEFT JOIN [Locations] loc ON loc.Id = e.LocationId
            LEFT JOIN [StaffProfile] sp ON sp.Id = e.OrganizerId
            LEFT JOIN [User] organizerUser ON organizerUser.Id = sp.UserId
            LEFT JOIN [Topics] t ON t.Id = e.TopicId
            WHERE e.Status != 'Deleted' AND e.PublishedAt IS NOT NULL
            ORDER BY e.UpdatedAt DESC
            """,
            fallback_query=f"""
            SELECT TOP ({top_n})
                e.Id AS EventId,
                e.Title,
                e.Description,
                e.Status,
                e.Type,
                e.Mode,
                e.StartTime,
                e.EndTime,
                e.MaxCapacity,
                e.MeetingUrl,
                CAST(NULL AS NVARCHAR(255)) AS DepartmentName,
                CAST(NULL AS NVARCHAR(255)) AS SemesterName,
                CAST(NULL AS NVARCHAR(255)) AS LocationName,
                CAST(NULL AS NVARCHAR(255)) AS OrganizerName,
                CAST(NULL AS NVARCHAR(255)) AS TopicName,
                e.IsDepositRequired,
                e.DepositAmount,
                e.PublishedAt,
                0 AS WaitlistCount,
                0 AS TeamMemberCount,
                0 AS AgendaCount,
                0 AS RegisteredCount,
                NULL AS AvgRating,
                0 AS FeedbackCount,
                e.CreatedAt,
                e.UpdatedAt
            FROM [Event] e
            ORDER BY e.UpdatedAt DESC
            """,
            query_name="event_rows",
        )

        # 2. Event agenda (program schedule)
        agenda_rows = self._fetch_rows_with_fallback(
            primary_query=f"""
            SELECT TOP ({top_n})
                ea.Id AS AgendaId,
                ea.EventId,
                e.Title AS EventTitle,
                ea.SessionName AS AgendaTitle,
                ea.Description AS AgendaDescription,
                ea.StartTime,
                ea.EndTime,
                ea.Location,
                ea.CreatedAt
            FROM [EventAgenda] ea
            LEFT JOIN [Event] e ON e.Id = ea.EventId
            ORDER BY ea.StartTime DESC
            """,
            fallback_query=f"""
            SELECT TOP ({top_n})
                ea.Id AS AgendaId,
                ea.EventId,
                CAST(NULL AS NVARCHAR(500)) AS EventTitle,
                ea.SessionName AS AgendaTitle,
                ea.Description AS AgendaDescription,
                ea.StartTime,
                ea.EndTime,
                ea.Location,
                ea.CreatedAt
            FROM [EventAgenda] ea
            ORDER BY ea.StartTime DESC
            """,
            query_name="agenda_rows",
        )

        # 3. Event team members (competing teams in competitions)
        team_rows = self._fetch_rows_with_fallback(
            primary_query=f"""
            SELECT TOP ({top_n})
                et.Id AS TeamId,
                et.EventId,
                e.Title AS EventTitle,
                et.TeamName AS MemberName,
                et.Description,
                et.Score,
                et.PlaceRank,
                et.CreatedAt
            FROM [EventTeam] et
            LEFT JOIN [Event] e ON e.Id = et.EventId
            ORDER BY et.Score DESC, et.PlaceRank ASC
            """,
            fallback_query=f"""
            SELECT TOP ({top_n})
                et.Id AS TeamId,
                et.EventId,
                CAST(NULL AS NVARCHAR(500)) AS EventTitle,
                et.TeamName AS MemberName,
                et.Description,
                et.Score,
                et.PlaceRank,
                et.CreatedAt
            FROM [EventTeam] et
            ORDER BY et.Score DESC, et.PlaceRank ASC
            """,
            query_name="team_rows",
        )

        feedback_rows = self._fetch_rows_with_fallback(
            primary_query=f"""
            SELECT TOP ({top_n})
                f.Id AS FeedbackId,
                f.EventId,
                e.Title AS EventTitle,
                f.StudentId,
                u.FullName AS StudentName,
                sp.StudentCode,
                f.Rating,
                f.Comment,
                f.CreatedAt,
                f.UpdatedAt
            FROM [Feedback] f
            LEFT JOIN [Event] e ON e.Id = f.EventId
            LEFT JOIN [StudentProfile] sp ON sp.Id = f.StudentId
            LEFT JOIN [User] u ON u.Id = sp.UserId
            ORDER BY f.CreatedAt DESC
            """,
            fallback_query=f"""
            SELECT TOP ({top_n})
                f.Id AS FeedbackId,
                f.EventId,
                CAST(NULL AS NVARCHAR(500)) AS EventTitle,
                f.StudentId,
                CAST(NULL AS NVARCHAR(255)) AS StudentName,
                CAST(NULL AS NVARCHAR(50)) AS StudentCode,
                f.Rating,
                f.Comment,
                f.CreatedAt,
                f.UpdatedAt
            FROM [Feedback] f
            ORDER BY f.CreatedAt DESC
            """,
            query_name="feedback_rows",
        )

        log_rows = self._fetch_rows_with_fallback(
            primary_query=f"""
            SELECT TOP ({top_n})
                l.Id AS LogId,
                l.StatusCode,
                l.ExceptionType,
                l.ExceptionMessage,
                l.Source,
                l.UserId,
                u.FullName AS UserName,
                l.CreatedAt,
                l.UpdatedAt
            FROM [SystemErrorLog] l
            LEFT JOIN [User] u ON u.Id = l.UserId
            ORDER BY l.CreatedAt DESC
            """,
            fallback_query=f"""
            SELECT TOP ({top_n})
                l.Id AS LogId,
                l.StatusCode,
                l.ExceptionType,
                l.ExceptionMessage,
                l.Source,
                l.UserId,
                CAST(NULL AS NVARCHAR(255)) AS UserName,
                l.CreatedAt,
                l.UpdatedAt
            FROM [SystemErrorLog] l
            ORDER BY l.CreatedAt DESC
            """,
            query_name="log_rows",
        )

        agg_feedback_rows = self._fetch_rows_with_fallback(
            primary_query="""
            SELECT TOP (200)
                e.Id AS EventId,
                e.Title AS EventTitle,
                COUNT(f.Id) AS FeedbackCount,
                AVG(CAST(f.Rating AS FLOAT)) AS AvgRating,
                SUM(CASE WHEN f.Rating >= 4 THEN 1 ELSE 0 END) AS HighRatingCount,
                SUM(CASE WHEN f.Rating <= 2 THEN 1 ELSE 0 END) AS LowRatingCount,
                MAX(f.CreatedAt) AS LastFeedbackAt
            FROM [Event] e
            LEFT JOIN [Feedback] f ON f.EventId = e.Id
            GROUP BY e.Id, e.Title
            ORDER BY AvgRating DESC, FeedbackCount DESC
            """,
            fallback_query="""
            SELECT TOP (200)
                CAST(NULL AS NVARCHAR(450)) AS EventId,
                CAST(NULL AS NVARCHAR(500)) AS EventTitle,
                COUNT(f.Id) AS FeedbackCount,
                AVG(CAST(f.Rating AS FLOAT)) AS AvgRating,
                SUM(CASE WHEN f.Rating >= 4 THEN 1 ELSE 0 END) AS HighRatingCount,
                SUM(CASE WHEN f.Rating <= 2 THEN 1 ELSE 0 END) AS LowRatingCount,
                MAX(f.CreatedAt) AS LastFeedbackAt
            FROM [Feedback] f
            GROUP BY f.EventId
            ORDER BY AvgRating DESC, FeedbackCount DESC
            """,
            query_name="agg_feedback_rows",
        )

        agg_log_rows = self._fetch_rows_with_fallback(
            primary_query="""
            SELECT TOP (200)
                COALESCE(CAST(StatusCode AS VARCHAR(10)), 'NA') AS StatusCode,
                COALESCE(ExceptionType, 'Unknown') AS ExceptionType,
                COUNT(*) AS ErrorCount,
                MAX(CreatedAt) AS LastSeenAt
            FROM [SystemErrorLog]
            GROUP BY StatusCode, ExceptionType
            ORDER BY ErrorCount DESC
            """,
            fallback_query="""
            SELECT TOP (200)
                COALESCE(CAST(StatusCode AS VARCHAR(10)), 'NA') AS StatusCode,
                COALESCE(ExceptionType, 'Unknown') AS ExceptionType,
                COUNT(*) AS ErrorCount,
                MAX(CreatedAt) AS LastSeenAt
            FROM [SystemErrorLog]
            GROUP BY StatusCode, ExceptionType
            ORDER BY ErrorCount DESC
            """,
            query_name="agg_log_rows",
        )

        chunks: list[Chunk] = []

        # Build event chunks with enhanced information
        for row in event_rows:
            capacity_used = row.get('RegisteredCount', 0) or 0
            max_capacity = row.get('MaxCapacity', 0) or 1
            capacity_percent = int((capacity_used / max_capacity) * 100) if max_capacity > 0 else 0
            
            avg_rating = row.get('AvgRating')
            rating_str = f"{self._format_float(avg_rating)}/5" if avg_rating else "Chưa có đánh giá"
            
            status_indicator = self._get_event_quality_indicator(avg_rating, row.get('Status'))
            
            text = (
                f"[SỰ KIỆN] Tên: {row.get('Title')} | Trạng thái: {row.get('Status')} | Loại: {row.get('Type')} | "
                f"Hình thức: {row.get('Mode')} | Phòng ban: {row.get('DepartmentName')} | Kỳ học: {row.get('SemesterName')} | "
                f"Địa điểm: {row.get('LocationName')} | Người tổ chức: {row.get('OrganizerName')} | "
                f"Bắt đầu: {row.get('StartTime')} | Kết thúc: {row.get('EndTime')} | "
                f"Sức chứa: {capacity_used}/{max_capacity} ({capacity_percent}%) | "
                f"Đánh giá: {rating_str} {status_indicator} | Ý kiến: {row.get('FeedbackCount')} | "
                f"Danh sách chờ: {row.get('WaitlistCount')} | Thành viên: {row.get('TeamMemberCount')} | "
                f"Chương trình: {row.get('AgendaCount')} | URL họp: {row.get('MeetingUrl')} | "
                f"Mô tả: {row.get('Description')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "event",
                        "event_id": row.get("EventId"),
                        "title": row.get("Title"),
                        "status": str(row.get("Status")),
                        "type": str(row.get("Type")),
                        "mode": str(row.get("Mode")),
                        "organizer": row.get("OrganizerName"),
                        "avg_rating": self._format_float(row.get("AvgRating")),
                        "registered": capacity_used,
                        "max_capacity": max_capacity,
                        "feedback_count": row.get('FeedbackCount', 0),
                        "start_time": self._safe_iso(row.get("StartTime")),
                        "end_time": self._safe_iso(row.get("EndTime")),
                        "updated_at": self._safe_iso(row.get("UpdatedAt")),
                    },
                )
            )

        # Build event agenda chunks
        for row in agenda_rows:
            text = (
                f"[CHƯƠNG TRÌNH SỰ KIỆN] Sự kiện: {row.get('EventTitle')} | "
                f"Tiêu đề: {row.get('AgendaTitle')} | "
                f"Thời gian: {row.get('StartTime')} - {row.get('EndTime')} | "
                f"Địa điểm: {row.get('Location')} | "
                f"Mô tả: {row.get('AgendaDescription')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "event_agenda",
                        "agenda_id": row.get("AgendaId"),
                        "event_id": row.get("EventId"),
                        "event_title": row.get("EventTitle"),
                        "title": row.get("AgendaTitle"),
                        "start_time": self._safe_iso(row.get("StartTime")),
                    },
                )
            )

        # Build event team chunks
        for row in team_rows:
            text = (
                f"[THÀNH VIÊN SỰ KIỆN] Sự kiện: {row.get('EventTitle')} | "
                f"Tên: {row.get('MemberName')} | "
                f"Mô tả: {row.get('Description')} | "
                f"Điểm số: {row.get('Score')} | "
                f"Xếp hạng: {row.get('PlaceRank')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "event_team",
                        "team_id": row.get("TeamId"),
                        "event_id": row.get("EventId"),
                        "event_title": row.get("EventTitle"),
                        "member_name": row.get("MemberName"),
                        "score": row.get("Score"),
                        "place_rank": row.get("PlaceRank"),
                    },
                )
            )

        for row in feedback_rows:
            text = (
                f"[FEEDBACK] Sự kiện: {row.get('EventTitle')} | Sinh viên: {row.get('StudentName')} ({row.get('StudentCode')}) | "
                f"Đánh giá: {row.get('Rating')}/5 | Ý kiến: {row.get('Comment')} | Ngày: {row.get('CreatedAt')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "feedback",
                        "feedback_id": row.get("FeedbackId"),
                        "event_id": row.get("EventId"),
                        "event_title": row.get("EventTitle"),
                        "rating": row.get("Rating"),
                        "created_at": self._safe_iso(row.get("CreatedAt")),
                    },
                )
            )

        for row in log_rows:
            text = (
                f"[SYSTEM_LOG] StatusCode: {row.get('StatusCode')} | ExceptionType: {row.get('ExceptionType')} | "
                f"Message: {row.get('ExceptionMessage')} | Source: {row.get('Source')} | "
                f"User: {row.get('UserName')} | CreatedAt: {row.get('CreatedAt')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "system_log",
                        "log_id": row.get("LogId"),
                        "status_code": row.get("StatusCode"),
                        "exception_type": row.get("ExceptionType"),
                        "created_at": self._safe_iso(row.get("CreatedAt")),
                    },
                )
            )

        for row in agg_feedback_rows:
            event_title = row.get('EventTitle') or 'Unknown'
            avg_rating = row.get('AvgRating')
            quality = "Chất lượng cao ⭐" if avg_rating and avg_rating >= 4 else ("Chất lượng trung bình" if avg_rating and avg_rating >= 3 else "Cần cải thiện")
            
            text = (
                f"[PHÂN TÍCH FEEDBACK] Sự kiện: {event_title} | "
                f"Số lượng ý kiến: {row.get('FeedbackCount')} | "
                f"Đánh giá TB: {self._format_float(avg_rating)}/5 {quality} | "
                f"Ý kiến tích cực: {row.get('HighRatingCount')} | "
                f"Ý kiến tiêu cực: {row.get('LowRatingCount')} | "
                f"Feedback gần nhất: {row.get('LastFeedbackAt')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "feedback_analytics",
                        "event_id": row.get("EventId"),
                        "event_title": event_title,
                        "avg_rating": self._format_float(avg_rating),
                        "feedback_count": row.get("FeedbackCount"),
                    },
                )
            )

        for row in agg_log_rows:
            text = (
                f"[PHÂN TÍCH LOG] Mã lỗi: {row.get('StatusCode')} | Loại lỗi: {row.get('ExceptionType')} | "
                f"Số lần xảy ra: {row.get('ErrorCount')} | Lần cuối cùng: {row.get('LastSeenAt')}"
            )
            chunks.append(
                Chunk(
                    text=text,
                    meta={
                        "doc_type": "log_analytics",
                        "status_code": row.get("StatusCode"),
                        "exception_type": row.get("ExceptionType"),
                        "error_count": row.get("ErrorCount"),
                    },
                )
            )

        return chunks

    @staticmethod
    def _safe_iso(value: Any) -> str | None:
        if isinstance(value, datetime):
            return value.isoformat()
        if value is None:
            return None
        return str(value)

    @staticmethod
    def _format_float(value: Any) -> str:
        if value is None:
            return "NA"
        try:
            return f"{float(value):.2f}"
        except Exception:
            return str(value)

    def _encode_texts(self, texts: list[str]) -> np.ndarray:
        vectors = self._embedding_model.encode(
            texts,
            convert_to_numpy=True,
            normalize_embeddings=True,
            show_progress_bar=False,
        )
        return vectors.astype(np.float32)

    def rebuild_index(self) -> None:
        chunks = self._build_chunks_from_db()
        if not chunks:
            raise ValueError("Không có dữ liệu để build KB")

        texts = [chunk.text for chunk in chunks]
        vectors = self._encode_texts(texts)

        dim = vectors.shape[1]
        index = faiss.IndexFlatIP(dim)
        index.add(vectors)

        self._chunks = chunks
        self._index = index
        self._last_reload = datetime.now().isoformat()

        self._write_index_atomic(index)
        self._write_json_atomic(
            self.chunks_path,
            [{"text": c.text, "meta": self._to_json_compatible(c.meta)} for c in chunks],
        )
        self._write_json_atomic(
            self.meta_path,
            {
                "embedding_model": self.embedding_model_name,
                "llm_model": self.llm_model_name,
                "kb_chunks": len(chunks),
                "updated_at": self._last_reload,
            },
            indent=2,
        )

    def _write_json_atomic(self, path: Path, payload: Any, *, indent: int | None = None) -> None:
        tmp_path = path.with_suffix(f"{path.suffix}.tmp")
        with tmp_path.open("w", encoding="utf-8") as f:
            json.dump(payload, f, ensure_ascii=False, indent=indent)
        tmp_path.replace(path)

    def _to_json_compatible(self, value: Any) -> Any:
        if isinstance(value, dict):
            return {key: self._to_json_compatible(item) for key, item in value.items()}
        if isinstance(value, list):
            return [self._to_json_compatible(item) for item in value]
        if isinstance(value, tuple):
            return [self._to_json_compatible(item) for item in value]
        if isinstance(value, Decimal):
            return int(value) if value == value.to_integral_value() else float(value)
        if isinstance(value, datetime):
            return value.isoformat()
        return value

    def _write_index_atomic(self, index: faiss.IndexFlatIP) -> None:
        tmp_path = self.index_path.with_suffix(f"{self.index_path.suffix}.tmp")
        faiss.write_index(index, str(tmp_path))
        tmp_path.replace(self.index_path)

    def _discard_persisted_index(self, reason: str) -> None:
        print(f"[RAG] Persisted KB cache is invalid: {reason}. Rebuilding from database.")
        for path in (self.index_path, self.chunks_path, self.meta_path):
            try:
                if path.exists():
                    path.unlink()
            except OSError as exc:
                print(f"[RAG] Failed to remove stale cache file {path.name}: {exc}")

    def _load_index_if_exists(self) -> bool:
        if not (self.index_path.exists() and self.chunks_path.exists()):
            return False

        try:
            index = faiss.read_index(str(self.index_path))
            with self.chunks_path.open("r", encoding="utf-8") as f:
                raw_chunks = json.load(f)
            chunks = [Chunk(text=item["text"], meta=item["meta"]) for item in raw_chunks]
        except (json.JSONDecodeError, KeyError, TypeError, ValueError, RuntimeError) as exc:
            self._discard_persisted_index(str(exc))
            return False

        self._index = index
        self._chunks = chunks
        self._last_reload = datetime.now().isoformat()
        return True

    def warmup(self) -> None:
        loaded = self._load_index_if_exists()
        if not loaded:
            self.rebuild_index()

    async def _auto_reload_loop(self) -> None:
        """Background task to automatically reload index at regular intervals"""
        print(f"[RAG] Auto-reload enabled: refreshing data every {self.auto_reload_interval} seconds")
        await asyncio.sleep(self.auto_reload_interval)  # Wait before first reload
        
        while not self._stop_auto_reload:
            try:
                print(f"[RAG] Auto-reloading index at {datetime.now().isoformat()}")
                self.rebuild_index()
                print(f"[RAG] Auto-reload completed. Next reload in {self.auto_reload_interval} seconds")
            except Exception as e:
                print(f"[RAG] Auto-reload failed: {e}")
            
            await asyncio.sleep(self.auto_reload_interval)

    def start_auto_reload(self) -> None:
        """Start the auto-reload background task"""
        if self._auto_reload_task is None or self._auto_reload_task.done():
            self._stop_auto_reload = False
            self._auto_reload_task = asyncio.create_task(self._auto_reload_loop())
            print(f"[RAG] Auto-reload task started")

    def stop_auto_reload(self) -> None:
        """Stop the auto-reload background task"""
        self._stop_auto_reload = True
        if self._auto_reload_task and not self._auto_reload_task.done():
            self._auto_reload_task.cancel()
            print(f"[RAG] Auto-reload task stopped")

    @staticmethod
    def _normalize_role(role: str) -> str:
        value = (role or "").strip().lower()
        if value in {"admin", "administrator"}:
            return "admin"
        if value in {"organizer", "approver", "staff"}:
            return "staff"
        return "user"

    @staticmethod
    def _get_event_quality_indicator(avg_rating: Any, status: str) -> str:
        """Get quality indicator emoji/text for event based on rating and status"""
        if status and str(status).lower() == "closed":
            return "✓ Đã kết thúc"
        if avg_rating is None:
            return ""
        try:
            rating = float(avg_rating)
            if rating >= 4.5:
                return "⭐⭐⭐⭐⭐ Xuất sắc"
            elif rating >= 4:
                return "⭐⭐⭐⭐ Rất tốt"
            elif rating >= 3:
                return "⭐⭐⭐ Tốt"
            elif rating >= 2:
                return "⭐⭐ Bình thường"
            else:
                return "⚠️ Cần cải thiện"
        except:
            return ""

    def _allowed_doc_types(self, normalized_role: str) -> set[str]:
        if normalized_role == "admin":
            return {"event", "feedback", "feedback_analytics", "system_log", "log_analytics", "event_agenda", "event_team"}
        if normalized_role == "staff":
            return {"event", "feedback", "feedback_analytics", "event_agenda", "event_team"}
        return {"event", "feedback_analytics", "event_agenda"}

    def _get_session_context(self, session_id: Optional[str]) -> str:
        """Get conversation history context for the session"""
        if not session_id or session_id not in self._sessions:
            return ""
        
        messages = self._sessions[session_id]
        if not messages:
            return ""
        
        # Get last 4 messages for context
        recent_messages = messages[-4:]
        context_lines = []
        for msg in recent_messages:
            role_text = "Người dùng" if msg.role == "user" else "Cố vấn"
            context_lines.append(f"{role_text}: {msg.content[:300]}")
        
        return "\n".join(context_lines)

    def _load_session_context_from_db(self, session_id: Optional[str], user_id: Optional[str]) -> None:
        """Load persisted chatbot messages into current in-memory session if needed."""
        if not session_id or not user_id or not self.db_service:
            return
        if session_id in self._sessions and self._sessions[session_id]:
            return

        try:
            rows = self.db_service.get_session_history(
                session_id=session_id,
                user_id=user_id,
                limit_messages=12,
            )
            if not rows:
                return

            hydrated_messages: list[ConversationMessage] = []
            for row in rows:
                sender = str(row.get("sender", "")).lower()
                role = "assistant" if sender == "assistant" else "user"
                raw_timestamp = row.get("created_at")
                timestamp = raw_timestamp.isoformat() if isinstance(raw_timestamp, datetime) else str(raw_timestamp)
                hydrated_messages.append(
                    ConversationMessage(
                        role=role,
                        content=str(row.get("content", "")),
                        timestamp=timestamp,
                    )
                )

            self._sessions[session_id] = hydrated_messages
        except Exception as exc:
            print(f"[RAG] Failed to hydrate session from DB: {exc}")

    def _get_user_personal_history_context(self, user_id: Optional[str], current_session_id: Optional[str]) -> str:
        """Get cross-session user context for personalized responses."""
        if not user_id or not self.db_service:
            return ""

        try:
            rows = self.db_service.get_user_recent_history(
                user_id=user_id,
                limit_messages=8,
                exclude_session_id=current_session_id,
            )
            if not rows:
                return ""

            context_lines = []
            for row in rows:
                sender = str(row.get("sender", "")).lower()
                role_text = "Người dùng" if sender == "user" else "Cố vấn"
                content = str(row.get("content", "")).strip()
                if not content:
                    continue
                context_lines.append(f"{role_text}: {content[:260]}")

            return "\n".join(context_lines)
        except Exception as exc:
            print(f"[RAG] Failed to load user personalized history: {exc}")
            return ""

    def retrieve(self, question: str, top_k: int, role: str) -> list[dict[str, Any]]:
        if self._index is None or not self._chunks:
            self.warmup()

        normalized_role = self._normalize_role(role)
        allowed_doc_types = self._allowed_doc_types(normalized_role)

        query_vec = self._encode_texts([question])
        search_k = min(max(top_k * 5, top_k), len(self._chunks))
        scores, indices = self._index.search(query_vec, search_k)

        results: list[dict[str, Any]] = []
        for score, idx in zip(scores[0], indices[0]):
            if idx < 0 or idx >= len(self._chunks):
                continue
            chunk = self._chunks[idx]
            if chunk.meta.get("doc_type") not in allowed_doc_types:
                continue
            results.append(
                {
                    "score": float(score),
                    "text": chunk.text,
                    "meta": chunk.meta,
                }
            )
            if len(results) >= top_k:
                break
        return results

    def _build_fallback_answer(
        self,
        question: str,
        retrieved: list[dict[str, Any]],
        normalized_role: str,
        error: Exception,
    ) -> str:
        type_counts: dict[str, int] = {}
        for item in retrieved:
            doc_type = str(item.get("meta", {}).get("doc_type", "unknown"))
            type_counts[doc_type] = type_counts.get(doc_type, 0) + 1

        top_items = retrieved[: min(3, len(retrieved))]
        evidence_lines = [f"- {item['text'][:220]}" for item in top_items]

        if normalized_role == "admin":
            role_scope = "Dữ liệu đang xét gồm sự kiện, feedback và log hệ thống (theo quyền admin)."
        elif normalized_role == "staff":
            role_scope = "Dữ liệu đang xét gồm sự kiện và feedback (không bao gồm chi tiết log hệ thống)."
        else:
            role_scope = "Dữ liệu đang xét ở mức an toàn cho người dùng, chủ yếu là sự kiện và feedback tổng hợp."

        error_type, diagnosis = self._categorize_llm_error(error)
        if error_type == "access_denied_network":
            action_hint = (
                "- Kiểm tra mạng outbound tới https://api.groq.com (VPN/Proxy/Firewall có thể đang chặn).\n"
                "- Nếu đang đặt HTTP_PROXY/HTTPS_PROXY, thử tắt proxy và gọi lại.\n"
                "- Xác nhận API key còn hoạt động bằng cách gọi endpoint /llm/health."
            )
        elif error_type == "auth":
            action_hint = (
                "- Kiểm tra lại GROQ_API_KEY trong file .env.\n"
                "- Tạo key mới trên Groq Console nếu key cũ bị thu hồi.\n"
                "- Gọi endpoint /llm/health để xác nhận trạng thái xác thực."
            )
        elif error_type == "model":
            action_hint = (
                f"- Đổi RAG_LLM_MODEL sang model khả dụng cho key hiện tại.\n"
                "- Gọi endpoint /llm/health để xác nhận model/network."
            )
        else:
            action_hint = (
                "- Kiểm tra GROQ_API_KEY và mạng Internet.\n"
                "- Gọi endpoint /llm/health để lấy chẩn đoán nhanh trước khi thử lại."
            )

        return (
            "1) Chẩn đoán nhanh\n"
            f"- Chưa thể gọi LLM bên ngoài ({str(error)}), nên hệ thống chuyển sang tư vấn fallback từ dữ liệu truy xuất.\n"
            f"- Kết luận kỹ thuật: {diagnosis}\n"
            f"- {role_scope}\n\n"
            "2) Bằng chứng từ dữ liệu\n"
            f"- Số nguồn truy xuất: {len(retrieved)}\n"
            f"- Phân bố nguồn: {type_counts}\n"
            + "\n".join(evidence_lines)
            + "\n\n"
            "3) Khuyến nghị ưu tiên\n"
            "- Ưu tiên kiểm tra các sự kiện/feedback xuất hiện ở top nguồn vì độ liên quan cao.\n"
            + action_hint
            + "\n\n"
            "4) Cảnh báo rủi ro\n"
            "- Khi đang ở chế độ fallback, chất lượng tổng hợp ngữ nghĩa có thể thấp hơn chế độ LLM đầy đủ."
        )

    def answer(self, question: str, top_k: int, role: str, session_id: Optional[str] = None, user_id: Optional[str] = None, user_profile_context: Optional[str] = None, save_to_db: bool = True) -> tuple[str, list[dict[str, Any]], str]:
        """
        Answer a question with RAG and conversation context.
        Returns: (answer, sources, session_id)
        """
        normalized_role = self._normalize_role(role)
        retrieved = self.retrieve(question=question, top_k=top_k, role=normalized_role)
        
        if not retrieved:
            answer = "Hiện tại chưa có dữ liệu phù hợp để tư vấn. Vui lòng thử câu hỏi cụ thể hơn về các sự kiện trong hệ thống."
            return answer, [], session_id or str(uuid4())

        # Create or reuse session
        if not session_id:
            session_id = str(uuid4())
            self._sessions[session_id] = []
        elif session_id not in self._sessions:
            self._sessions[session_id] = []

        if save_to_db and user_id and self.db_service:
            try:
                session_id = self.db_service.ensure_chat_session(user_id=user_id, session_id=session_id)
            except Exception as exc:
                print(f"[RAG] Failed to ensure DB chatbot session: {exc}")

        self._load_session_context_from_db(session_id=session_id, user_id=user_id)
        if session_id not in self._sessions:
            self._sessions[session_id] = []

        # Build context with conversation history
        context = "\n\n".join(
            [f"[Độ liên quan: {item['score']:.2%}] {item['text']}" for item in retrieved]
        )
        
        conversation_history = self._get_session_context(session_id)
        user_personal_history = self._get_user_personal_history_context(
            user_id=user_id,
            current_session_id=session_id,
        )

        system_prompt = (
            "Bạn là một trợ lý AI thông minh, hỗ trợ sinh viên với thông tin sự kiện tại AEMS.\n\n"
            
            "CHẾ ĐỘ HOẠT ĐỘNG (ƯU TIÊN THEO THỨ TỰ):\n"
            "0. 🎯 ƯU TIÊN CAO NHẤT - NẾU người dùng CHÀO HỎI (xin chào, hi, hello, chào bạn, hey, v.v.):\n"
            "   → Chào lại thân thiện như bạn bè\n"
            "   → Giới thiệu: 'Mình là trợ lý AI của hệ thống AEMS, chuyên hỗ trợ thông tin về các sự kiện, phản hồi và phân tích liên quan đến AEMS'\n"
            "   → Hỏi người dùng cần giúp gì\n"
            "   → KHÔNG cần tìm kiếm hoặc đề cập sự kiện cụ thể trong lời chào\n\n"
            "1. NẾU câu hỏi liên quan đến SỰ KIỆN:\n"
            "   → Ưu tiên sử dụng dữ liệu liên quan được cung cấp\n"
            "   → Khuyến nghị sự kiện có đánh giá cao (⭐⭐⭐⭐ trở lên)\n"
            "   → Giải thích thời gian, địa điểm, cách tham gia từ dữ liệu\n"
            "   → Cảnh báo nếu gần hết chỗ hoặc có lưu ý đặc biệt\n\n"
            "2. NẾU câu hỏi KHÔNG liên quan đến sự kiện:\n"
            "   → Trả lời bình thường dựa trên kiến thức chung\n"
            "   → Có thể đề cập đến sự kiện AEMS nếu phù hợp\n\n"
            
            "PHONG CÁCH TRẢ LỜI:\n"
            "- Ngắn gọn, dễ hiểu, thân thiện, chuyên nghiệp\n"
            "- Khi nói về sự kiện: chỉ đề cập 2-3 sự kiện nổi bật nhất\n"
            "- KHÔNG dump data, trích xuất thông tin cần thiết\n"
            "- Sử dụng emoji phù hợp: ⭐📅📍💡❓\n"
            "- Tránh lặp lại thông tin không cần thiết\n\n"
            
            "QUY TẮC DỮ LIỆU:\n"
            "- Chỉ chia sẻ thông tin có trong dữ liệu sự kiện (không bịa đặt)\n"
            "- Nếu không có dữ liệu phù hợp về sự kiện → nói: 'Không tìm thấy sự kiện phù hợp'\n"
            "- Không hiển thị ID, timestamp kỹ thuật, metadata nội bộ\n"
        )

        if normalized_role == "admin":
            role_hint = "Bạn đang nói với Admin - có thể chia sẻ tổng quan hệ thống, phân tích sâu, và dữ liệu kỹ thuật."
        elif normalized_role == "staff":
            role_hint = "Bạn đang nói với Staff/Tổ chức sự kiện - tập trung vào quản lý sự kiện, feedback, và chất lượng."
        else:
            role_hint = "Bạn đang nói với Sinh viên/Người dùng - chỉ chia sẻ thông tin công khai, ưu tiên sự kiện chất lượng cao."

        user_prompt = (
            f"{role_hint}\n\n"
        )
        
        if conversation_history:
            user_prompt += (
                f"Lịch sử hội thoại:\n{conversation_history}\n\n"
            )

        if user_personal_history:
            user_prompt += (
                f"Ngữ cảnh cá nhân từ các phiên trước của cùng người dùng:\n{user_personal_history}\n\n"
            )

        if user_profile_context:
            user_prompt += (
                f"Hồ sơ người dùng hiện tại (lấy từ hệ thống):\n{user_profile_context}\n\n"
            )
        
        user_prompt += (
            f"Câu hỏi: {question}\n\n"
            f"Dữ liệu liên quan:\n{context}\n\n"
            "Hãy trả lời ngắn gọn (2-3 sự kiện nổi bật), dễ hiểu, và hữu ích."
        )

        can_call_llm, llm_block_reason = self._can_call_llm()
        if not can_call_llm:
            ex = RuntimeError(llm_block_reason or "LLM is currently unavailable")
            self._record_llm_error(ex)
            fallback_answer = self._build_fallback_answer(
                question=question,
                retrieved=retrieved,
                normalized_role=normalized_role,
                error=ex,
            )
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=fallback_answer))

            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=fallback_answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save unavailable-LLM fallback to DB: {e}")

            return fallback_answer, retrieved, session_id

        try:
            completion = self._llm.chat.completions.create(
                model=self.llm_model_name,
                temperature=0.2,
                max_tokens=800,
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
            )

            answer = completion.choices[0].message.content or ""
            answer = answer.strip()
            self._clear_llm_error()
            
            # Save to conversation history
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=answer))
            
            # Save to database if requested
            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save conversation to DB: {e}")
            
            return answer, retrieved, session_id
        except Exception as ex:
            self._record_llm_error(ex)
            fallback_answer = self._build_fallback_answer(
                question=question,
                retrieved=retrieved,
                normalized_role=normalized_role,
                error=ex,
            )
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=fallback_answer))
            
            # Save to database if requested
            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=fallback_answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save fallback conversation to DB: {e}")
            
            return fallback_answer, retrieved, session_id

    async def answer_stream(self, question: str, top_k: int, role: str, session_id: Optional[str] = None, user_id: Optional[str] = None, user_profile_context: Optional[str] = None, save_to_db: bool = True) -> AsyncGenerator[str, None]:
        """
        Answer a question with streaming tokens.
        Yields: JSON lines with streaming content
        """
        normalized_role = self._normalize_role(role)
        retrieved = self.retrieve(question=question, top_k=top_k, role=normalized_role)
        
        if not retrieved:
            yield json.dumps({
                "type": "answer",
                "content": "Hiện tại chưa có dữ liệu phù hợp để tư vấn. Vui lòng thử câu hỏi cụ thể hơn về các sự kiện trong hệ thống.",
                "session_id": session_id or str(uuid4())
            }) + "\n"
            return

        # Create or reuse session
        if not session_id:
            session_id = str(uuid4())
            self._sessions[session_id] = []
        elif session_id not in self._sessions:
            self._sessions[session_id] = []

        if save_to_db and user_id and self.db_service:
            try:
                session_id = self.db_service.ensure_chat_session(user_id=user_id, session_id=session_id)
            except Exception as exc:
                print(f"[RAG] Failed to ensure DB chatbot session: {exc}")

        self._load_session_context_from_db(session_id=session_id, user_id=user_id)
        if session_id not in self._sessions:
            self._sessions[session_id] = []

        # Build context
        context = "\n\n".join(
            [f"[Độ liên quan: {item['score']:.2%}] {item['text']}" for item in retrieved]
        )
        
        conversation_history = self._get_session_context(session_id)
        user_personal_history = self._get_user_personal_history_context(
            user_id=user_id,
            current_session_id=session_id,
        )

        system_prompt = (
            "Bạn là một trợ lý AI thông minh, hỗ trợ sinh viên với thông tin sự kiện tại AEMS.\n\n"
            
            "CHẾ ĐỘ HOẠT ĐỘNG (ƯU TIÊN THEO THỨ TỰ):\n"
            "0. 🎯 ƯU TIÊN CAO NHẤT - NẾU người dùng CHÀO HỎI (xin chào, hi, hello, chào bạn, hey, v.v.):\n"
            "   → Chào lại thân thiện như bạn bè\n"
            "   → Giới thiệu: 'Mình là trợ lý AI của hệ thống AEMS, chuyên hỗ trợ thông tin về các sự kiện, phản hồi và phân tích liên quan đến AEMS'\n"
            "   → Hỏi người dùng cần giúp gì\n"
            "   → KHÔNG cần tìm kiếm hoặc đề cập sự kiện cụ thể trong lời chào\n\n"
            "1. NẾU câu hỏi liên quan đến SỰ KIỆN:\n"
            "   → Ưu tiên sử dụng dữ liệu liên quan được cung cấp\n"
            "   → Khuyến nghị sự kiện có đánh giá cao (⭐⭐⭐⭐ trở lên)\n"
            "   → Giải thích thời gian, địa điểm, cách tham gia từ dữ liệu\n"
            "   → Cảnh báo nếu gần hết chỗ hoặc có lưu ý đặc biệt\n\n"
            "2. NẾU câu hỏi KHÔNG liên quan đến sự kiện:\n"
            "   → Trả lời bình thường dựa trên kiến thức chung\n"
            "   → Có thể đề cập đến sự kiện AEMS nếu phù hợp\n\n"
            
            "PHONG CÁCH TRẢ LỜI:\n"
            "- Ngắn gọn, dễ hiểu, thân thiện, chuyên nghiệp\n"
            "- Khi nói về sự kiện: chỉ đề cập 2-3 sự kiện nổi bật nhất\n"
            "- KHÔNG dump data, trích xuất thông tin cần thiết\n"
            "- Sử dụng emoji phù hợp: ⭐📅📍💡❓\n"
            "- Tránh lặp lại thông tin không cần thiết\n\n"
            
            "QUY TẮC DỮ LIỆU:\n"
            "- Chỉ chia sẻ thông tin có trong dữ liệu sự kiện (không bịa đặt)\n"
            "- Nếu không có dữ liệu phù hợp về sự kiện → nói: 'Không tìm thấy sự kiện phù hợp'\n"
            "- Không hiển thị ID, timestamp kỹ thuật, metadata nội bộ\n"
        )

        if normalized_role == "admin":
            role_hint = "Bạn đang nói với Admin - có thể chia sẻ tổng quan hệ thống, phân tích sâu, và dữ liệu kỹ thuật."
        elif normalized_role == "staff":
            role_hint = "Bạn đang nói với Staff/Tổ chức sự kiện - tập trung vào quản lý sự kiện, feedback, và chất lượng."
        else:
            role_hint = "Bạn đang nói với Sinh viên/Người dùng - chỉ chia sẻ thông tin công khai, ưu tiên sự kiện chất lượng cao."

        user_prompt = (
            f"{role_hint}\n\n"
        )
        
        if conversation_history:
            user_prompt += (
                f"Lịch sử hội thoại:\n{conversation_history}\n\n"
            )

        if user_personal_history:
            user_prompt += (
                f"Ngữ cảnh cá nhân từ các phiên trước của cùng người dùng:\n{user_personal_history}\n\n"
            )

        if user_profile_context:
            user_prompt += (
                f"Hồ sơ người dùng hiện tại (lấy từ hệ thống):\n{user_profile_context}\n\n"
            )
        
        user_prompt += (
            f"Câu hỏi: {question}\n\n"
            f"Dữ liệu liên quan:\n{context}\n\n"
            "Hãy trả lời ngắn gọn (2-3 sự kiện nổi bật), dễ hiểu, và hữu ích."
        )

        can_call_llm, llm_block_reason = self._can_call_llm()
        if not can_call_llm:
            ex = RuntimeError(llm_block_reason or "LLM is currently unavailable")
            self._record_llm_error(ex)
            fallback_answer = self._build_fallback_answer(
                question=question,
                retrieved=retrieved,
                normalized_role=normalized_role,
                error=ex,
            )
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=fallback_answer))

            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=fallback_answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save stream unavailable-LLM fallback: {e}")

            yield json.dumps({
                "type": "error",
                "content": fallback_answer,
            }) + "\n"
            return

        try:
            # Stream response from LLM
            stream = self._llm.chat.completions.create(
                model=self.llm_model_name,
                temperature=0.2,
                max_tokens=800,
                stream=True,
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
            )

            full_answer = ""
            self._clear_llm_error()
            
            # First yield session info
            yield json.dumps({
                "type": "session",
                "session_id": session_id
            }) + "\n"

            # Stream tokens
            for chunk in stream:
                if chunk.choices[0].delta.content:
                    token = chunk.choices[0].delta.content
                    full_answer += token
                    yield json.dumps({
                        "type": "token",
                        "content": token
                    }) + "\n"

            # Yield sources at the end
            yield json.dumps({
                "type": "sources",
                "sources": [
                    {
                        "score": f"{s['score']:.2%}",
                        "meta": s["meta"],
                    }
                    for s in retrieved
                ]
            }) + "\n"

            # Save to conversation history
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=full_answer))
            
            # Save to database if requested
            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=full_answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save streaming conversation to DB: {e}")

            # Yield end signal
            yield json.dumps({
                "type": "done"
            }) + "\n"

        except Exception as ex:
            self._record_llm_error(ex)
            fallback_answer = self._build_fallback_answer(
                question=question,
                retrieved=retrieved,
                normalized_role=normalized_role,
                error=ex,
            )
            self._sessions[session_id].append(ConversationMessage(role="user", content=question))
            self._sessions[session_id].append(ConversationMessage(role="assistant", content=fallback_answer))
            
            # Save to database if requested
            if save_to_db and user_id and self.db_service:
                try:
                    saved_session_id, _, _ = self.db_service.save_conversation_batch(
                        user_id=user_id,
                        question=question,
                        answer=fallback_answer,
                        session_id=session_id,
                        role=normalized_role,
                    )
                    session_id = saved_session_id
                except Exception as e:
                    print(f"[RAG] Failed to save error fallback to DB: {e}")
            
            yield json.dumps({
                "type": "error",
                "content": fallback_answer
            }) + "\n"

    def stats(self) -> dict[str, Any]:
        log_chunks = sum(1 for c in self._chunks if c.meta.get("doc_type") in {"system_log", "log_analytics"})
        return {
            "kb_chunks": len(self._chunks),
            "log_chunks": log_chunks,
            "kb_index_size": int(self._index.ntotal if self._index is not None else 0),
            "log_index_size": log_chunks,
            "model": self.embedding_model_name,
            "llm": self.llm_model_name,
            "llm_enabled": self._llm is not None,
            "llm_last_error_type": self._llm_last_error_type,
            "llm_last_error": self._llm_last_error,
            "api_version": "role-aware-rag-v2",
            "last_reload": self._last_reload,
        }

engine = RagEngine()


@asynccontextmanager
async def lifespan(_: FastAPI) -> AsyncGenerator[None, None]:
    """Initialize and clean up the engine lifecycle."""
    print("[RAG] Starting up...")
    engine.warmup()
    engine.start_auto_reload()
    print("[RAG] Startup complete. Auto-reload is active.")
    try:
        yield
    finally:
        print("[RAG] Shutting down...")
        engine.stop_auto_reload()
        print("[RAG] Shutdown complete.")


app = FastAPI(title="AEMS Smart Event Chatbot API", version="2.0.0", lifespan=lifespan)


@app.get("/health")
def health() -> dict[str, Any]:
    return {
        "status": "ok",
        "kb_size": len(engine._chunks),
        "log_size": sum(
            1
            for c in engine._chunks
            if c.meta.get("doc_type") in {"system_log", "log_analytics"}
        ),
        "auto_reload_enabled": not engine._stop_auto_reload,
        "auto_reload_interval": engine.auto_reload_interval,
        "last_reload": engine._last_reload,
    }


@app.get("/stats")
def stats() -> dict[str, Any]:
    return engine.stats()


@app.get("/llm/health")
def llm_health() -> dict[str, Any]:
    return engine.probe_llm_health()


@app.post("/reload")
def reload_index() -> dict[str, Any]:
    """Manually trigger a reload of the knowledge base"""
    engine.rebuild_index()
    return {"status": "reloaded", **engine.stats()}


@app.post("/auto-reload/enable")
async def enable_auto_reload() -> dict[str, Any]:
    """Enable automatic reloading"""
    engine.start_auto_reload()
    return {
        "status": "enabled",
        "interval_seconds": engine.auto_reload_interval,
        "message": f"Auto-reload enabled. Data will refresh every {engine.auto_reload_interval} seconds"
    }


@app.post("/auto-reload/disable")
def disable_auto_reload() -> dict[str, Any]:
    """Disable automatic reloading"""
    engine.stop_auto_reload()
    return {
        "status": "disabled",
        "message": "Auto-reload disabled. Data will only update on manual /reload or server restart"
    }


@app.get("/auto-reload/status")
def auto_reload_status() -> dict[str, Any]:
    """Get auto-reload status"""
    return {
        "enabled": not engine._stop_auto_reload,
        "interval_seconds": engine.auto_reload_interval,
        "last_reload": engine._last_reload,
        "next_reload_in_seconds": engine.auto_reload_interval if not engine._stop_auto_reload else None
    }


@app.post("/ask")
def ask(request: AskRequest) -> dict[str, Any]:
    try:
        answer, sources, session_id = engine.answer(
            question=request.question,
            top_k=request.top_k or engine.default_top_k,
            role=request.role,
            session_id=request.session_id,
            user_id=request.user_id,
            user_profile_context=request.user_profile_context,
            save_to_db=request.save_to_db,
        )
        return {
            "session_id": session_id,
            "question": request.question,
            "answer": answer,
            "sources": [
                {
                    "score": f"{s['score']:.2%}",
                    "meta": s["meta"],
                }
                for s in sources
            ],
            "error": None,
        }
    except Exception as ex:
        return {
            "session_id": request.session_id or str(uuid4()),
            "question": request.question,
            "answer": "",
            "sources": [],
            "error": str(ex),
        }


@app.post("/ask-stream")
async def ask_stream(request: AskRequest) -> StreamingResponse:
    """Stream response tokens one by one"""
    async def event_generator() -> AsyncGenerator[str, None]:
        try:
            async for chunk in engine.answer_stream(
                question=request.question,
                top_k=request.top_k or engine.default_top_k,
                role=request.role,
                session_id=request.session_id,
                user_id=request.user_id,
                user_profile_context=request.user_profile_context,
                save_to_db=request.save_to_db,
            ):
                yield chunk
        except Exception as ex:
            yield json.dumps({
                "type": "error",
                "error": str(ex),
            }) + "\n"

    return StreamingResponse(event_generator(), media_type="application/json")


@app.get("/conversation/{session_id}")
def get_conversation_history(session_id: str) -> dict[str, Any]:
    """Get conversation history for a session"""
    if session_id not in engine._sessions:
        raise HTTPException(status_code=404, detail=f"Session {session_id} not found")
    
    messages = engine._sessions[session_id]
    return {
        "session_id": session_id,
        "message_count": len(messages),
        "messages": [
            {
                "role": msg.role,
                "content": msg.content,
                "timestamp": msg.timestamp,
            }
            for msg in messages
        ],
    }


@app.post("/conversation/{session_id}/clear")
def clear_conversation(session_id: str) -> dict[str, Any]:
    """Clear conversation history for a session"""
    if session_id in engine._sessions:
        del engine._sessions[session_id]
    return {"session_id": session_id, "status": "cleared"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000, reload=False)
