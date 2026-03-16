using DataAccess.Enum;

namespace BusinessLogic.DTOs.Student
{
    /// <summary>
    /// Full detail DTO for the student event detail page.
    /// </summary>
    public class StudentEventDetailDto
    {
        public string EventId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingUrl { get; set; }
        public EventModeEnum? Mode { get; set; }
        public EventStatusEnum Status { get; set; }
        public int MaxCapacity { get; set; }
        public int RegisteredCount { get; set; }
        public int AvailableSeats => Math.Max(0, MaxCapacity - RegisteredCount);
        public bool IsFull => AvailableSeats == 0;
        public bool? IsDepositRequired { get; set; }
        public decimal? DepositAmount { get; set; }
        public string? TopicName { get; set; }
        public string? SemesterName { get; set; }
        public string? DepartmentName { get; set; }
        public string? OrganizerName { get; set; }
        public string? OrganizerUserId { get; set; }
        public string? OrganizerAvatarUrl { get; set; }
        public string? OrganizerPosition { get; set; }

        // Computed for the action button
        public bool IsRegistered { get; set; }
        public bool CanRegister { get; set; }   // Published + future + not registered + not full
        public bool CanCancel { get; set; }      // Registered + event hasn't started

        // Ticket id for cancel action
        public string? TicketId { get; set; }

        // Waitlist state
        public bool IsInWaitlist { get; set; }
        public int? WaitlistPosition { get; set; }
        public DataAccess.Enum.EventWaitlistStatusEnum? WaitlistStatus { get; set; }
        public string? WaitlistStudentProfileId { get; set; }
        public FeedbackStatusEnum FeedbackStatus { get; set; } = FeedbackStatusEnum.NA;
        public bool HasSubmittedFeedback { get; set; }
        public int? CurrentFeedbackRating { get; set; }
        public string? CurrentFeedbackComment { get; set; }
        // Agendas (new)
        public List<EventAgendaItemDto>? Agendas { get; set; }
    }
    public class EventAgendaItemDto
    {
        public string? SessionName { get; set; }
        public string? Description { get; set; }
        public string? SpeakerName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Location { get; set; }
    }


}
