"""
Database service for saving chat sessions and messages to SQL Server.
Maps RAG conversation data to ChatSession and ChatMessage tables.
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

    def create_chat_session(
        self,
        user_id: str,
        title: Optional[str] = None,
    ) -> str:
        """
        Create a new chat session.
        Returns: session_id
        """
        session_id = str(uuid4())
        created_at = datetime.now().isoformat()

        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                INSERT INTO [dbo].[ChatSession]
                    ([Id], [UserId], [Title], [Status], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    session_id,
                    user_id,
                    title,
                    "Active",  # ChatSessionStatus.Active
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
            print(f"[DB] Failed to create chat session: {e}")
            raise

    def save_chat_message(
        self,
        session_id: str,
        sender: str,  # "user" or "assistant"
        content: str,
        error_message: Optional[str] = None,
        status: str = "Final",  # Streaming, Final, Error
    ) -> str:
        """
        Save a chat message to the database.
        Returns: message_id
        """
        message_id = str(uuid4())
        now = datetime.now().isoformat()

        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                INSERT INTO [dbo].[ChatMessage]
                    ([Id], [SessionId], [Sender], [Content], [Status], [ErrorMessage], [IsDeleted], [CreatedAt], [UpdatedAt])
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    message_id,
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
        title: Optional[str] = None,
    ) -> tuple[str, str, str]:
        """
        Save a complete conversation (question + answer) to the database.
        Creates a new session and saves both messages.
        
        Returns: (session_id, question_message_id, answer_message_id)
        """
        try:
            # Create session
            session_id = self.create_chat_session(
                user_id=user_id,
                title=title or question[:100],  # Use first 100 chars as title if not provided
            )

            # Save question
            question_msg_id = self.save_chat_message(
                session_id=session_id,
                sender="user",
                content=question,
                status="Final",
            )

            # Save answer
            answer_msg_id = self.save_chat_message(
                session_id=session_id,
                sender="assistant",
                content=answer,
                status="Final",
            )

            print(f"[DB] Conversation saved: session_id={session_id}")
            return session_id, question_msg_id, answer_msg_id

        except Exception as e:
            print(f"[DB] Failed to save conversation batch: {e}")
            raise

    def save_conversation_streaming(
        self,
        user_id: str,
        session_id: Optional[str],
        question: str,
        full_answer: str,
        title: Optional[str] = None,
    ) -> tuple[str, str, str]:
        """
        Save a streaming conversation to the database.
        Reuses or creates session ID and saves both messages.
        
        Returns: (session_id, question_message_id, answer_message_id)
        """
        try:
            # Create or verify session
            if not session_id:
                session_id = self.create_chat_session(
                    user_id=user_id,
                    title=title or question[:100],
                )
            else:
                # Session already exists (from previous streaming call)
                # Just verify ownership by updating timestamp
                conn = self._get_connection()
                cursor = conn.cursor()
                now = datetime.now().isoformat()
                cursor.execute(
                    "UPDATE [dbo].[ChatSession] SET [UpdatedAt] = ? WHERE [Id] = ?",
                    (now, session_id),
                )
                conn.commit()
                cursor.close()
                conn.close()

            # Save question if not already saved
            question_msg_id = self.save_chat_message(
                session_id=session_id,
                sender="user",
                content=question,
                status="Final",
            )

            # Save full answer
            answer_msg_id = self.save_chat_message(
                session_id=session_id,
                sender="assistant",
                content=full_answer,
                status="Final",
            )

            print(f"[DB] Streaming conversation saved: session_id={session_id}")
            return session_id, question_msg_id, answer_msg_id

        except Exception as e:
            print(f"[DB] Failed to save streaming conversation: {e}")
            raise

    def get_session_history(self, session_id: str) -> List[dict]:
        """
        Retrieve chat history for a session.
        Returns: List of messages with id, sender, content, created_at
        """
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                """
                SELECT [Id], [Sender], [Content], [Status], [CreatedAt]
                FROM [dbo].[ChatMessage]
                WHERE [SessionId] = ? AND [IsDeleted] = 0
                ORDER BY [CreatedAt] ASC
                """,
                (session_id,),
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
                    }
                )

            cursor.close()
            conn.close()
            return messages

        except Exception as e:
            print(f"[DB] Failed to retrieve session history: {e}")
            return []

    def archive_session(self, session_id: str) -> bool:
        """Archive a chat session"""
        try:
            conn = self._get_connection()
            cursor = conn.cursor()

            cursor.execute(
                "UPDATE [dbo].[ChatSession] SET [Status] = ?, [UpdatedAt] = ? WHERE [Id] = ?",
                ("Archived", datetime.now().isoformat(), session_id),
            )

            conn.commit()
            cursor.close()
            conn.close()
            return True

        except Exception as e:
            print(f"[DB] Failed to archive session: {e}")
            return False
