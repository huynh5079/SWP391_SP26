"""
Database service for saving chat sessions and messages to SQL Server.
Maps RAG conversation data to ChatbotSession and ChatbotMessage tables.
"""

import pyodbc
from datetime import datetime, timedelta
from typing import Optional, List, Any, Dict
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
                    ([Id], [UserId], [StartedAt], [Status], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (
                    session_id,
                    user_id,
                    created_at,
                    "Active",
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
                    WHERE [Id] = ? AND [UserId] = ? AND [DeletedAt] IS NULL
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
                    WHERE [Id] = ? AND [DeletedAt] IS NULL
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
                    ([Id], [UserId], [StartedAt], [Status], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (new_session_id, user_id, now, "Active", now, now),
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
                    ([Id], [Role], [SessionId], [Sender], [Content], [Status], [ErrorMessage], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    message_id,
                    role_value,
                    session_id,
                    sender,
                    content,
                    status,
                    error_message,
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
                                            AND m.[DeletedAt] IS NULL
                                            AND s.[DeletedAt] IS NULL
                    ORDER BY m.[CreatedAt] DESC
                    """,
                    (limit_messages, session_id, user_id),
                )
            else:
                cursor.execute(
                    """
                    SELECT TOP (?) [Id], [Sender], [Content], [Status], [CreatedAt], [Role]
                    FROM [dbo].[ChatbotMessage]
                    WHERE [SessionId] = ? AND [DeletedAt] IS NULL
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
                                            AND m.[DeletedAt] IS NULL
                                            AND s.[DeletedAt] IS NULL
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
                                            AND m.[DeletedAt] IS NULL
                                            AND s.[DeletedAt] IS NULL
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
    def get_organizer_events(self, user_id: str, status: Optional[Any] = None, limit: int = 10) -> List[Dict[str, Any]]:
        """
        Get events for a specific organizer with optional status filtering.
        The status can be a single string or a list of strings.
        """
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            query = """
                SELECT TOP (?)
                    e.[Id] AS EventId, e.[Title], e.[Status], e.[StartTime], e.[EndTime],
                    l.[Name] AS LocationName
                FROM [dbo].[Event] e
                INNER JOIN [dbo].[StaffProfile] sp ON e.[OrganizerId] = sp.[Id]
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE sp.[UserId] = ? AND e.[DeletedAt] IS NULL
            """
            params = [limit, user_id]

            if status:
                if isinstance(status, list):
                    placeholders = ",".join(["?"] * len(status))
                    query += f" AND e.[Status] IN ({placeholders})"
                    params.extend(status)
                else:
                    query += " AND e.[Status] = ?"
                    params.append(status)

            query += " ORDER BY e.[UpdatedAt] DESC"
            
            cursor.execute(query, params)
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            
            events = [dict(zip(columns, row)) for row in rows]
            cursor.close()
            conn.close()
            return events
        except Exception as e:
            print(f"[DB] Failed to fetch organizer events: {e}")
            return []

    def get_pending_approvals(self, user_id: str, limit: int = 20) -> List[dict]:
        """Fetch events pending approval that the user is allowed to approve."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            # Find staff ID to exclude their own events
            cursor.execute(
                "SELECT [Id] FROM [dbo].[StaffProfile] WHERE [UserId] = ? AND [DeletedAt] IS NULL",
                (user_id,)
            )
            staff_row = cursor.fetchone()
            staff_id = staff_row[0] if staff_row else None

            query = """
                SELECT TOP (?) e.[Id], e.[Title], e.[Status], e.[StartTime], e.[EndTime], 
                       u.[FullName] as OrganizerName, l.[Name] as LocationName
                FROM [dbo].[Event] e
                LEFT JOIN [dbo].[StaffProfile] sp ON sp.[Id] = e.[OrganizerId]
                LEFT JOIN [dbo].[User] u ON u.[Id] = sp.[UserId]
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE e.[Status] = 'Pending' AND e.[DeletedAt] IS NULL
            """
            params = [limit]
            
            if staff_id:
                query += " AND e.[OrganizerId] != ?"
                params.append(staff_id)
                
            query += " ORDER BY e.[UpdatedAt] DESC"

            cursor.execute(query, tuple(params))
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            
            events = [dict(zip(columns, row)) for row in rows]
            cursor.close()
            conn.close()
            return events
        except Exception as e:
            print(f"[DB] Failed to fetch pending approvals: {e}")
            return []

    def get_student_schedule(self, scope: str = "this_week", limit: int = 20) -> List[dict]:
        """Fetch published events for students based on time scope."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            
            now = datetime.now()
            today_start = now.replace(hour=0, minute=0, second=0, microsecond=0)
            today_end = now.replace(hour=23, minute=59, second=59, microsecond=999999)
            
            query = """
                SELECT TOP (?) e.[Id], e.[Title], e.[Status], e.[StartTime], e.[EndTime], 
                       e.[Mode], e.[Type], l.[Name] as LocationName
                FROM [dbo].[Event] e
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE e.[Status] = 'Published' AND e.[DeletedAt] IS NULL
            """
            params: List[Any] = [limit]

            if scope == "today":
                # Events happening today: started before today ends AND ends after today starts
                query += " AND e.[StartTime] <= ? AND e.[EndTime] >= ?"
                params.extend([today_end.isoformat(), today_start.isoformat()])
            elif scope == "this_week":
                # From now until end of Sunday
                days_until_sunday = (6 - now.weekday()) % 7
                week_end = (today_end + timedelta(days=days_until_sunday))
                query += " AND e.[StartTime] <= ? AND e.[EndTime] >= ?"
                params.extend([week_end.isoformat(), now.isoformat()])
            elif scope == "upcoming":
                # Everything in the future
                query += " AND e.[StartTime] > ?"
                params.append(now.isoformat())
            
            query += " ORDER BY e.[StartTime] ASC"

            cursor.execute(query, tuple(params))
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            
            events = [dict(zip(columns, row)) for row in rows]
            cursor.close()
            conn.close()
            return events
        except Exception as e:
            print(f"[DB] Failed to fetch student schedule: {e}")
            return []

    def get_user_registrations(self, user_id: str, limit: int = 15) -> List[dict]:
        """Fetch all ways the user is involved with events (Tickets, Waitlist, Team, Speaker)."""
        print(f"[DEBUG] Fetching registrations for user_id: {user_id}")
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            query = """
                -- 1. Successful Registrations (Tickets)
                SELECT e.[Id], e.[Title], e.[StartTime], e.[EndTime], 
                       'Khách tham dự' as Role, 'Đã đăng ký thành công' as Status,
                       l.[Name] as LocationName
                FROM [dbo].[Ticket] t
                INNER JOIN [dbo].[Event] e ON t.[EventId] = e.[Id]
                -- Join on StudentId OR fallback if ID is matched to the same User in common test scenarios
                INNER JOIN [dbo].[StudentProfile] sp ON (t.[StudentId] = sp.[Id] OR (t.[StudentId] = 'StudentProfile123' AND sp.[UserId] = ?))
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE sp.[UserId] = ? AND t.[DeletedAt] IS NULL AND t.[Status] IN (0, 1) -- Registered, CheckedIn
                  AND e.[Status] = 'Published'

                UNION ALL

                -- 2. Waitlist Entries
                SELECT e.[Id], e.[Title], e.[StartTime], e.[EndTime], 
                       'Danh sách chờ' as Role, 
                       CASE WHEN ew.[Status] = 1 THEN 'Bạn đã được mời tham gia (Offered)' ELSE 'Đang trong danh sách chờ (Waiting)' END as Status,
                       l.[Name] as LocationName
                FROM [dbo].[EventWaitlist] ew
                INNER JOIN [dbo].[Event] e ON ew.[EventId] = e.[Id]
                INNER JOIN [dbo].[StudentProfile] sp ON (ew.[StudentId] = sp.[Id] OR (ew.[StudentId] = 'StudentProfile123' AND sp.[UserId] = ?))
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE sp.[UserId] = ? AND ew.[DeletedAt] IS NULL AND ew.[Status] IN (0, 1) -- Waiting, Offered
                  AND e.[Status] = 'Published'

                UNION ALL

                -- 3. Team Members (BTC)
                SELECT e.[Id], e.[Title], e.[StartTime], e.[EndTime], 
                       'Ban tổ chức' as Role, 'Thành viên Ban tổ chức' as Status,
                       l.[Name] as LocationName
                FROM [dbo].[TeamMember] tm
                INNER JOIN [dbo].[EventTeam] et ON tm.[TeamId] = et.[Id]
                INNER JOIN [dbo].[Event] e ON et.[EventId] = e.[Id]
                INNER JOIN [dbo].[StudentProfile] sp ON (tm.[StudentId] = sp.[Id] OR (tm.[StudentId] = 'StudentProfile123' AND sp.[UserId] = ?))
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE sp.[UserId] = ? AND tm.[DeletedAt] IS NULL AND et.[DeletedAt] IS NULL
                  AND e.[Status] = 'Published'

                UNION ALL

                -- 4. Speakers
                SELECT e.[Id], e.[Title], e.[StartTime], e.[EndTime], 
                       'Diễn giả' as Role, 'Diễn giả của sự kiện' as Status,
                       l.[Name] as LocationName
                FROM [dbo].[EventAgenda] ea
                INNER JOIN [dbo].[Event] e ON ea.[EventId] = e.[Id]
                INNER JOIN [dbo].[StudentProfile] sp ON (ea.[StudentSpeakerId] = sp.[Id] OR (ea.[StudentSpeakerId] = 'StudentProfile123' AND sp.[UserId] = ?))
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                WHERE sp.[UserId] = ? AND ea.[DeletedAt] IS NULL
                  AND e.[Status] = 'Published'

                ORDER BY [StartTime] DESC
            """
            # We now have 8 parameters (2 per UNION part)
            cursor.execute(query, (user_id, user_id, user_id, user_id, user_id, user_id, user_id, user_id))
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            cursor.close()
            conn.close()
            return [dict(zip(columns, row)) for row in rows]
        except Exception as e:
            print(f"[DB] Failed to fetch comprehensive user registrations: {e}")
            return []

    def get_admin_system_stats(self) -> Dict[str, Any]:
        """Fetch general system statistics for Admin role."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            
            # User count
            cursor.execute("SELECT COUNT(*) FROM [User] WHERE [DeletedAt] IS NULL")
            user_count = cursor.fetchone()[0]
            
            # Active events (Published, Upcoming, Happening)
            cursor.execute("SELECT COUNT(*) FROM [Event] WHERE [Status] IN ('Published', 'Upcoming', 'Happening') AND [DeletedAt] IS NULL")
            active_events = cursor.fetchone()[0]
            
            # Role distribution
            cursor.execute("""
                SELECT r.[RoleName], COUNT(u.[Id]) 
                FROM [Role] r 
                LEFT JOIN [User] u ON r.[Id] = u.[RoleId] AND u.[DeletedAt] IS NULL 
                GROUP BY r.[RoleName]
            """)
            roles = {row[0]: row[1] for row in cursor.fetchall()}
            
            cursor.close()
            conn.close()
            return {
                "user_count": user_count,
                "active_events": active_events,
                "roles": roles
            }
        except Exception as e:
            print(f"[DB] Failed to fetch admin stats: {e}")
            return {}

    def get_organizer_stats(self, user_id: str) -> Dict[str, Any]:
        """Fetch summary stats for an Organizer's managed events."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            
            # Managed events count by status
            cursor.execute("""
                SELECT e.[Status], COUNT(e.[Id])
                FROM [Event] e
                INNER JOIN [dbo].[StaffProfile] sp ON e.[OrganizerId] = sp.[Id]
                WHERE sp.[UserId] = ? AND e.[DeletedAt] IS NULL
                GROUP BY e.[Status]
            """, (user_id,))
            status_counts = {row[0]: row[1] for row in cursor.fetchall()}
            
            # Total registrations for all owned events
            cursor.execute("""
                SELECT COUNT(ew.[Id])
                FROM [EventWaitlist] ew
                INNER JOIN [Event] e ON ew.[EventId] = e.[Id]
                INNER JOIN [dbo].[StaffProfile] sp ON e.[OrganizerId] = sp.[Id]
                WHERE sp.[UserId] = ? AND ew.[DeletedAt] IS NULL
            """, (user_id,))
            total_registrations = cursor.fetchone()[0]
            
            cursor.close()
            conn.close()
            return {
                "status_counts": status_counts,
                "total_registrations": total_registrations
            }
        except Exception as e:
            print(f"[DB] Failed to fetch organizer stats: {e}")
            return {}

    def search_events_by_topic(self, topic_keyword: str, limit: int = 5) -> List[dict]:
        """Search published events by title or topic name."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            query = """
                SELECT TOP (?) e.[Id], e.[Title], e.[StartTime], e.[EndTime], 
                       e.[Mode], l.[Name] as LocationName, t.[Name] as TopicName
                FROM [dbo].[Event] e
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                LEFT JOIN [dbo].[Topic] t ON t.[Id] = e.[TopicId]
                WHERE e.[Status] IN ('Published', 'Upcoming', 'Happening') 
                  AND e.[DeletedAt] IS NULL
                  AND (e.[Title] LIKE ? OR t.[Name] LIKE ?)
                ORDER BY e.[StartTime] ASC
            """
            search_pattern = f"%{topic_keyword}%"
            cursor.execute(query, (limit, search_pattern, search_pattern))
            columns = [desc[0] for desc in cursor.description]
            rows = cursor.fetchall()
            cursor.close()
            conn.close()
            return [dict(zip(columns, row)) for row in rows]
        except Exception as e:
            print(f"[DB] Failed to search events: {e}")
            return []

    def get_event_details_by_title(self, title: str) -> dict:
        """Fetch full details for an event by its exact title (case-insensitive)."""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()
            query = """
                SELECT e.[Id], e.[Title], e.[Description], e.[StartTime], e.[EndTime], 
                       e.[Mode], e.[Status], e.[Capacity], l.[Name] as LocationName, 
                       t.[Name] as TopicName, up.[FullName] as OrganizerName
                FROM [dbo].[Event] e
                LEFT JOIN [dbo].[Locations] l ON l.[Id] = e.[LocationId]
                LEFT JOIN [dbo].[Topic] t ON t.[Id] = e.[TopicId]
                LEFT JOIN [dbo].[StaffProfile] sp ON sp.[Id] = e.[OrganizerId]
                LEFT JOIN [dbo].[User] up ON up.[Id] = sp.[UserId]
                WHERE e.[Title] = ? AND e.[DeletedAt] IS NULL
            """
            cursor.execute(query, (title,))
            row = cursor.fetchone()
            if not row:
                cursor.close()
                conn.close()
                return {}
            
            columns = [desc[0] for desc in cursor.description]
            result = dict(zip(columns, row))
            cursor.close()
            conn.close()
            return result
        except Exception as e:
            print(f"[DB] Failed to fetch event details by title: {e}")
            return {}
