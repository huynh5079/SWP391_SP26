using BusinessLogic.DTOs.Student;

namespace BusinessLogic.Service.Student
{
    public interface IStudentEventService
    {
        /// <summary>
        /// Returns published events for the weekly calendar.
        /// Optionally filtered by search term, topic, or semester.
        /// </summary>
        Task<List<StudentEventBrowseDto>> GetPublishedEventsAsync(
            string studentId,
            string? search = null,
            string? topicId = null,
            string? semesterId = null);

        /// <summary>
        /// Returns dashboard statistics for a student.
        /// </summary>
        Task<StudentDashboardStatsDto> GetDashboardStatsAsync(string studentId);

        /// <summary>
        /// Full event detail with per-student registration state.
        /// </summary>
        Task<StudentEventDetailDto> GetEventDetailAsync(string eventId, string studentId);

        /// <summary>
        /// Register a student for an event (creates a Ticket).
        /// Throws if event is in the past, already full, or student already registered.
        /// If full, calls AddToWaitlistAsync (stub for future implementation).
        /// </summary>
        Task RegisterForEventAsync(string studentId, string eventId);

        /// <summary>
        /// Cancel a student's registration (soft-deletes the Ticket).
        /// Only allowed while the event hasn't started.
        /// </summary>
        Task CancelRegistrationAsync(string studentId, string ticketId);

        /// <summary>
        /// Returns all events where the student is registered, part of the team, or a speaker.
        /// </summary>
        Task<List<StudentEventBrowseDto>> GetMyParticipationsAsync(string studentId);

        /// <summary>
        /// Submit feedback during or after an event starts.
        /// </summary>
        Task SubmitFeedbackAsync(string studentId, string eventId, SubmitFeedbackRequestDto dto);

        /// <summary>
        /// Returns all feedback entries for an event to display to students.
        /// </summary>
        Task<List<StudentEventFeedbackItemDto>> GetEventFeedbacksAsync(string eventId);

        // ──────────────────────────────────────────────
        // Waitlist stub — to be implemented in a future phase
        // ──────────────────────────────────────────────
        Task AddToWaitlistAsync(string studentId, string eventId);
    }
}
