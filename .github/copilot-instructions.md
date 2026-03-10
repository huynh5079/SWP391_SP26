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

## Room Addressing
- Display and store room addresses with explicit labels, including Building, Floor, and Room, instead of bare values.
- Normalize room addresses to avoid duplicate labels, such as 'Building Building B'; preserve a single explicit label for Building, Floor, and Room.

## User Management
- Implement 'soft delete' for users by changing the user's status rather than relying solely on DeletedAt; admin deletions may have a time limit for automatic reactivation.