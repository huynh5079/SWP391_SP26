"""
Database service for saving chat sessions and messages to SQL Server.
Maps RAG conversation data to ChatbotSession and ChatbotMessage tables.
"""

import pyodbc
from datetime import datetime
from typing import Optional, List
from uuid import uuid4


class DatabaseService:
    """Service to save chat sessions and messages to SQL Server"""

    def __init__(self, connection_string: str):
        self.connection_string = connection_string

    def _get_connection(self):
        """Get a database connection"""
        return pyodbc.connect(self.connection_string)

    def _utc_now_iso(self) -> str:
        return datetime.utcnow().isoformat()

    def _normalize_role(self, role: Optional[str]) -> str:
        if not role:
            return "Student"
        normalized = role.strip().lower()
        if normalized == "admin":
            return "Admin"
        if normalized in {"organizer", "staff"}:
            return "Organizer"
        if normalized == "approver":
            return "Approver"
        return "Student"

    def create_chat_session(
        self,
        user_id: str,
        session_id: Optional[str] = None,
    ) -> str:
        """
        Create a new chatbot session.
        Returns: session_id
        """
        session_id = session_id or str(uuid4())
        created_at = self._utc_now_iso()

        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                INSERT INTO [dbo].[ChatbotSession]
                    ([Id], [UserId], [StartedAt], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    session_id,
                    user_id,
                    created_at,
                    "Active",
                    False,
                    created_at,
                    created_at,
                ),
            )

            conn.commit()
            cursor.close()
            conn.close()

            return session_id
        except Exception as e:
            print(f"[DB] Failed to create chatbot session: {e}")
            raise

    def ensure_chat_session(self, user_id: str, session_id: Optional[str] = None) -> str:
        """Get a valid chatbot session for user, creating one when needed."""
        now = self._utc_now_iso()

        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            if session_id:
                cursor.execute(
                    """
                    SELECT TOP 1 [Id]
                    FROM [dbo].[ChatbotSession]
                    WHERE [Id] = ? AND [UserId] = ? AND [IsDeleted] = 0
                    """,
                    (session_id, user_id),
                )
                row = cursor.fetchone()
                if row:
                    cursor.execute(
                        """
                        UPDATE [dbo].[ChatbotSession]
                        SET [UpdatedAt] = ?, [Status] = ?
                        WHERE [Id] = ?
                        """,
                        (now, "Active", session_id),
                    )
                    conn.commit()
                    cursor.close()
                    conn.close()
                    return session_id

                cursor.execute(
                    """
                    SELECT TOP 1 [Id]
                    FROM [dbo].[ChatbotSession]
                    WHERE [Id] = ? AND [IsDeleted] = 0
                    """,
                    (session_id,),
                )
                owned_by_other = cursor.fetchone()
                if owned_by_other:
                    raise ValueError("Session does not belong to this user")

            new_session_id = session_id or str(uuid4())
            cursor.execute(
                """
                INSERT INTO [dbo].[ChatbotSession]
                    ([Id], [UserId], [StartedAt], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (new_session_id, user_id, now, "Active", False, now, now),
            )
            conn.commit()
            cursor.close()
            conn.close()
            return new_session_id
        except Exception as e:
            print(f"[DB] Failed to ensure chatbot session: {e}")
            raise

    def save_chat_message(
        self,
        session_id: str,
        sender: str,  # "user" or "assistant"
        content: str,
        role: Optional[str] = None,
        error_message: Optional[str] = None,
        status: str = "Final",  # Streaming, Final, Error
    ) -> str:
        """
        Save a chat message to the database.
        Returns: message_id
        """
        message_id = str(uuid4())
        now = self._utc_now_iso()
        role_value = self._normalize_role(role)

        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                INSERT INTO [dbo].[ChatbotMessage]
                    ([Id], [Role], [SessionId], [Sender], [Content], [Status], [ErrorMessage], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    message_id,
                    role_value,
                    session_id,
                    sender,
                    content,
                    status,
                    error_message,
                    False,
                    now,
                    now,
                ),
            )

            conn.commit()
            cursor.close()
            conn.close()

            return message_id
        except Exception as e:
            print(f"[DB] Failed to save chat message: {e}")
            raise

    def save_conversation_batch(
        self,
        user_id: str,
        question: str,
        answer: str,
        session_id: Optional[str] = None,
        role: Optional[str] = None,
    ) -> tuple[str, str, str]:
        """
        Save a complete conversation (question + answer) to the database.
        Reuses session when provided, otherwise creates a new one.
        
        Returns: (session_id, question_message_id, answer_message_id)
        """
        try:
            ensured_session_id = self.ensure_chat_session(user_id=user_id, session_id=session_id)

            question_msg_id = self.save_chat_message(
                session_id=ensured_session_id,
                sender="user",
                content=question,
                role=role,
                status="Final",
            )

            answer_msg_id = self.save_chat_message(
                session_id=ensured_session_id,
                sender="assistant",
                content=answer,
                role=role,
                status="Final",
            )

            print(f"[DB] Conversation saved: session_id={ensured_session_id}")
            return ensured_session_id, question_msg_id, answer_msg_id

        except Exception as e:
            print(f"[DB] Failed to save conversation batch: {e}")
            raise

    def save_conversation_streaming(
        self,
        user_id: str,
        session_id: Optional[str],
        question: str,
        full_answer: str,
        role: Optional[str] = None,
    ) -> tuple[str, str, str]:
        """
        Save a streaming conversation to the database.
        Reuses or creates session ID and saves both messages.
        
        Returns: (session_id, question_message_id, answer_message_id)
        """
        try:
            return self.save_conversation_batch(
                user_id=user_id,
                question=question,
                answer=full_answer,
                session_id=session_id,
                role=role,
            )

        except Exception as e:
            print(f"[DB] Failed to save streaming conversation: {e}")
            raise

    def get_session_history(self, session_id: str, user_id: Optional[str] = None, limit_messages: int = 20) -> List[dict]:
        """
        Retrieve chatbot history for a session.
        Returns: List of messages with id, sender, content, created_at
        """
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            if user_id:
                cursor.execute(
                    """
                    SELECT TOP (?) m.[Id], m.[Sender], m.[Content], m.[Status], m.[CreatedAt], m.[Role]
                    FROM [dbo].[ChatbotMessage] m
                    INNER JOIN [dbo].[ChatbotSession] s ON s.[Id] = m.[SessionId]
                    WHERE m.[SessionId] = ?
                      AND s.[UserId] = ?
                      AND m.[IsDeleted] = 0
                      AND s.[IsDeleted] = 0
                    ORDER BY m.[CreatedAt] DESC
                    """,
                    (limit_messages, session_id, user_id),
                )
            else:
                cursor.execute(
                    """
                    SELECT TOP (?) [Id], [Sender], [Content], [Status], [CreatedAt], [Role]
                    FROM [dbo].[ChatbotMessage]
                    WHERE [SessionId] = ? AND [IsDeleted] = 0
                    ORDER BY [CreatedAt] DESC
                    """,
                    (limit_messages, session_id),
                )

            messages = []
            for row in cursor.fetchall():
                messages.append(
                    {
                        "id": row[0],
                        "sender": row[1],
                        "content": row[2],
                        "status": row[3],
                        "created_at": row[4],
                        "role": row[5],
                    }
                )

            cursor.close()
            conn.close()
            return list(reversed(messages))

        except Exception as e:
            print(f"[DB] Failed to retrieve session history: {e}")
            return []

    def get_user_recent_history(
        self,
        user_id: str,
        limit_messages: int = 12,
        exclude_session_id: Optional[str] = None,
    ) -> List[dict]:
        """Retrieve recent chatbot messages across sessions for personalization context."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            if exclude_session_id:
                cursor.execute(
                    """
                    SELECT TOP (?) m.[Id], m.[SessionId], m.[Sender], m.[Content], m.[Status], m.[CreatedAt], m.[Role]
                    FROM [dbo].[ChatbotMessage] m
                    INNER JOIN [dbo].[ChatbotSession] s ON s.[Id] = m.[SessionId]
                    WHERE s.[UserId] = ?
                      AND m.[IsDeleted] = 0
                      AND s.[IsDeleted] = 0
                      AND s.[Id] <> ?
                    ORDER BY m.[CreatedAt] DESC
                    """,
                    (limit_messages, user_id, exclude_session_id),
                )
            else:
                cursor.execute(
                    """
                    SELECT TOP (?) m.[Id], m.[SessionId], m.[Sender], m.[Content], m.[Status], m.[CreatedAt], m.[Role]
                    FROM [dbo].[ChatbotMessage] m
                    INNER JOIN [dbo].[ChatbotSession] s ON s.[Id] = m.[SessionId]
                    WHERE s.[UserId] = ?
                      AND m.[IsDeleted] = 0
                      AND s.[IsDeleted] = 0
                    ORDER BY m.[CreatedAt] DESC
                    """,
                    (limit_messages, user_id),
                )

            rows = cursor.fetchall()
            cursor.close()
            conn.close()

            history = []
            for row in rows:
                history.append(
                    {
                        "id": row[0],
                        "session_id": row[1],
                        "sender": row[2],
                        "content": row[3],
                        "status": row[4],
                        "created_at": row[5],
                        "role": row[6],
                    }
                )

            return list(reversed(history))
        except Exception as e:
            print(f"[DB] Failed to retrieve user recent history: {e}")
            return []

    def archive_session(self, session_id: str) -> bool:
        """Archive a chatbot session"""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                UPDATE [dbo].[ChatbotSession]
                SET [Status] = ?, [EndedAt] = ?, [UpdatedAt] = ?
                WHERE [Id] = ?
                """,
                ("Archived", self._utc_now_iso(), self._utc_now_iso(), session_id),
            )

            conn.commit()
            cursor.close()
            conn.close()
            return True

        except Exception as e:
            print(f"[DB] Failed to archive session: {e}")
            return False
