using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Event : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string Title { get; set; } = null!;

    public EventStatusAvailableEnum StatusEventAvailable;
	public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }
   
    public string? OrganizerId { get; set; }

    public string? SemesterId { get; set; }

    public string? DepartmentId { get; set; }

    public string? LocationId { get; set; }

    public virtual Location? Location { get; set; }

    public string? TopicId { get; set; }

    public virtual Topic? Topic { get; set; }

    // Mode of the event (Offline/Online/Hybrid)
    public EventModeEnum? Mode { get; set; }

    // If online or hybrid, meeting URL (nullable)
    public string? MeetingUrl { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int MaxCapacity { get; set; }

    public bool? IsDepositRequired { get; set; }

    public decimal? DepositAmount { get; set; }

    public EventTypeEnum? Type { get; set; }

    public EventStatusEnum Status { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();

    public virtual ICollection<BudgetProposal> BudgetProposals { get; set; } = new List<BudgetProposal>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<EventAgenda> EventAgenda { get; set; } = new List<EventAgenda>();

    public virtual ICollection<EventDocument> EventDocuments { get; set; } = new List<EventDocument>();

    public virtual ICollection<EventQuiz> EventQuizzes { get; set; } = new List<EventQuiz>();

    public virtual ICollection<EventReminder> EventReminders { get; set; } = new List<EventReminder>();

    public virtual ICollection<EventTeam> EventTeams { get; set; } = new List<EventTeam>();

    public virtual ICollection<EventWaitlist> EventWaitlists { get; set; } = new List<EventWaitlist>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual StaffProfile? Organizer { get; set; }

    public virtual Semester? Semester { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
