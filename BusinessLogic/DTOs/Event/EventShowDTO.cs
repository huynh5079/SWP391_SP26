using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role.Organizer;

public class EventAgendaDto
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string SessionName { get; set; } = "";
    public string? Description { get; set; }
    public string? SpeakerName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
}

public class EventDocumentDto
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string? FileName { get; set; }
    public string? Url { get; set; }
    public string? Type { get; set; }
}

public class EventDetailsDto
{
    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public string? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? LocationId { get; set; }
    public string? Location { get; set; }
    public string? TopicId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public bool IsDepositRequired { get; set; }
    public decimal DepositAmount { get; set; }
    public EventTypeEnum? Type { get; set; }
    public EventStatusEnum Status { get; set; }
    public EventModeEnum? Mode { get; set; }
    public string? MeetingUrl { get; set; }
    public int RegisteredCount { get; set; }
    public int CheckedInCount { get; set; }
    public int WaitlistCount { get; set; }
    public double AvgRating { get; set; }
    public List<EventAgendaDto> Agendas { get; set; } = new();
    public List<EventDocumentDto> Documents { get; set; } = new();

    // Permission flags
    public bool CanEdit { get; set; }
    public bool CanSendForApproval { get; set; }

    // Latest approval info
    public ApprovalActionEnum? LastApprovalAction { get; set; }
    public string? LastApprovalComment { get; set; }
    public DateTime? LastApprovalAt { get; set; }
}

public class EventItemDto
{
	public string? OrganizerId { get; set; }
	public string? OrganizerName { get; set; }
	public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public EventStatusEnum Status { get; set; }

	// Optional UI fields
	public DateTime? EndTime { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Location { get; set; }
    public string? TimeState { get; set; }
    public string? LastApprovalComment { get; set; }
}

// DTO for listing events in organizer area
public class EventListDto
{
    public string OrganizerId { get; set; } = "";
    public string? OrganizerName { get; set; }
    public string? OrganizerCode { get; set; }

    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }

    public string? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public string? SemesterCode { get; set; }

    public string? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }

    public string? TopicId { get; set; }
    public string? TopicName { get; set; }

    public string? LocationId { get; set; }
    public string? Location { get; set; }
    public string? LocationType { get; set; }
    public string? MeetingUrl { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public DateTime? RegistrationOpenTime { get; set; }
    public DateTime? RegistrationCloseTime { get; set; }

    public int MaxCapacity { get; set; }

    public EventStatusEnum Status { get; set; }
    public string? Type { get; set; }
    public string? Mode { get; set; }

    public bool IsDepositRequired { get; set; }
    public decimal DepositAmount { get; set; }

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int RegisteredCount { get; set; }
    public int CheckedInCount { get; set; }
    public int WaitlistCount { get; set; }
    public double AvgRating { get; set; }
    public int FeedbackCount { get; set; }

    // Last approval info (recent approver action for UI)
    public ApprovalActionEnum? LastApprovalAction { get; set; }
    public DateTime? LastApprovalActionAt { get; set; }
    public string? LastApprovalBy { get; set; }

    public string TimeState { get; set; } = "";
    public int RemainingCapacity { get; set; }

    public bool CanEdit { get; set; }
    public bool CanSendForApproval { get; set; }
    public bool CanPublish { get; set; }
    public bool CanCancel { get; set; }
    public bool IsFull { get; set; }
    public bool IsRegistrationOpen { get; set; }
    public bool HasThumbnail { get; set; }

    // (kept above)
    public string? LastRejectReason { get; set; }

    public bool HasQuiz { get; set; }
    public string? QuizId { get; set; }
    public int QuizQuestionCount { get; set; }
    public int QuizAttemptCount { get; set; }
    public int QuizPassedCount { get; set; }
    public bool? QuizIsActive { get; set; }
    public int? QuizPassingScore { get; set; }

    public bool HasBudget { get; set; }
    public int BudgetProposalCount { get; set; }
    public decimal BudgetTotalAmount { get; set; }
    public decimal ExpenseTotalAmount { get; set; }
    public int ReceiptCount { get; set; }

    public int AgendaCount { get; set; }
    public int DocumentCount { get; set; }

    public int ReminderCount { get; set; }
    public int NotificationCount { get; set; }
    public int UnreadNotificationCount { get; set; }
}

public class EventListItemDto
{
    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public EventStatusEnum Status { get; set; } 
}
public class EventWaitListDTO
    {
    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime StartTime { get; set; }
    public int WaitlistCount { get; set; }
}

