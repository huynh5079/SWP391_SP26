# Copilot Instructions

## Project Guidelines
- The existing chatbot is the AI user-support feature and must remain separate from the new human-to-human role chat UI; do not remove or replace the chatbot when adding role-based messaging.

## Event Registration Workflow
- Student registers for an event.
- Registration status is set to Pending.
- Approver reviews and approves the student's ticket/registration.
- Upon approval, the system generates the ticket and QR code.
- Organizer scans the QR code for check-in.

## Role Responsibilities
- The Approver is responsible for creating and managing topics, creating rooms/locations, changing their status, and approving event registrations.
- The Organizer is responsible for managing tickets, facilitating check-in, and seeing which rooms are available for a given time.
- Allow one organizer to have multiple QuizSet records; do not enforce one QuizSet per organizer.

## Room Addressing
- Display and store room addresses with explicit labels, including Building, Floor, and Room, instead of bare values.
- Normalize room addresses to avoid duplicate labels, such as 'Building Building B'; preserve a single explicit label for Building, Floor, and Room.

## User Management
- Implement 'soft delete' for users by changing the user's status rather than relying solely on DeletedAt; admin deletions may have a time limit for automatic reactivation.

## Testing Guidelines
- Use minimal test implementations that only cover the explicitly requested scenario, instead of full mock-heavy setups.
- Load test data from JSON files when writing tests for this project.

## Event Management
- Apply the requested two-column layout and agenda management changes to `DetailEvent.cshtml`, not `CreateEvent.cshtml`.

## Quiz Schema
- For the quiz schema in this project, remove direct EventQuiz-to-QuizQuestion linkage and StudentQuizScore.QuizId; keep StudentQuizScore linked only to EventQuiz while answers remain in StudentAnswer referencing QuestionBank. Answers should remain stored inside the question rather than in a separate Answer entity/table.
- Store status fields as strings in the database.

## Enum Management
- Prefer shared enums to live in the central enum file/class instead of being declared inside feature DTO/contracts files.