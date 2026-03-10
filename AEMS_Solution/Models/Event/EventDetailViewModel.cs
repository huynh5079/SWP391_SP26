using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Event
{
	// =========================
	// MAIN DETAILS VIEWMODEL
	// =========================
	public class EventDetailsViewModel
	{
		// ---- Core Event ----
		public string EventId { get; set; } = "";
		public string Title { get; set; } = "";
		public string? Description { get; set; }
		public string? ThumbnailUrl { get; set; }

		public string OrganizerId { get; set; } = "";
		public StaffMiniVm? Organizer { get; set; }

		public string? SemesterId { get; set; }
		public string? SemesterName { get; set; }
		public string? SemesterCode { get; set; }

		public string? DepartmentId { get; set; }
		public string? DepartmentName { get; set; }
		public string? DepartmentCode { get; set; }

		public string? LocationId { get; set; }
		public string? LocationName { get; set; }  // nếu bạn map từ Locations table
		public string? LocationText { get; set; }  // nếu Location trong Event là text

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public int MaxCapacity { get; set; }

		// Type / Status đang lưu dạng string trong DB (theo ERD)
		public string? Type { get; set; }      // EventTypeEnum.ToString()
		public EventStatusEnum Status { get; set; }  // EventStatusEnum
		
		public EventModeEnum? Mode { get; set; }
		public string? MeetingUrl { get; set; }

		public bool IsDepositRequired { get; set; }
		public decimal DepositAmount { get; set; }

		public DateTime? PublishedAt { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ---- Derived / UI helpers (không cần lưu DB) ----
		public string TimeState { get; set; } = "Upcoming"; // Upcoming/Happening/Completed (tính từ Start/End)
		public bool CanEdit { get; set; }
		public bool CanSendForApproval { get; set; }
		public bool CanPublish { get; set; }
		public bool CanCancel { get; set; }

		public ApprovalActionEnum? LastApprovalAction { get; set; }
		public string? LastApprovalComment { get; set; }
		public DateTime? LastApprovalAt { get; set; }

		// --- Convenience/stat fields (some views expect top-level props) ---
		public int RegisteredCount { get; set; }
		public int CheckedInCount { get; set; }
		public int WaitlistCount { get; set; }
		public double AvgRating { get; set; }

		// simple location text used by some views
		public string? Location { get; set; }

		// ---- Aggregations / Stats ----
		public EventStatsVm Stats { get; set; } = new();

		// ---- Child Modules ----
		public List<EventAgendaVm> Agendas { get; set; } = new();
		public List<EventDocumentVm> Documents { get; set; } = new();

		public List<TicketVm> Tickets { get; set; } = new();              // Organizer/Admin mới cần full list
		public List<CheckInHistoryVm> CheckIns { get; set; } = new();     // Organizer/Admin

		public List<EventWaitlistViewModel> Waitlist { get; set; } = new();           // Organizer/Admin
		public List<FeedbackVm> Feedbacks { get; set; } = new();          // public/role-based
		public QuizDetailVm? Quiz { get; set; }                           // nếu event có quiz

		public List<ApprovalLogVm> ApprovalLogs { get; set; } = new();    // cực quan trọng

		public List<BudgetProposalVm> Budgets { get; set; } = new();      // nếu có module
		public List<ExpenseReceiptVm> Receipts { get; set; } = new();     // nếu có module

		public List<EventTeamVm> Teams { get; set; } = new();             // nếu event kiểu team

		public List<EventReminderVm> Reminders { get; set; } = new();     // nếu organizer muốn xem
		public List<NotificationVm> Notifications { get; set; } = new();  // nếu bạn muốn show theo event/user
	}

	// =========================
	// STATS
	// =========================
	public class EventStatsVm
	{
		public int RegisteredCount { get; set; }
		public int CheckedInCount { get; set; }
		public int WaitlistCount { get; set; }
		public double AvgRating { get; set; }

		public int RemainingCapacity { get; set; } // MaxCapacity - RegisteredCount
		public int FeedbackCount { get; set; }
		public int QuizAttempts { get; set; }      // count StudentQuizScore
		public int PassedCount { get; set; }
	}

	// =========================
	// LOOKUP MINI VMS
	// =========================
	public class StaffMiniVm
	{
		public string StaffId { get; set; } = "";  // StaffProfile.Id
		public string UserId { get; set; } = "";
		public string? StaffCode { get; set; }
		public string? Position { get; set; }
		public string? DepartmentId { get; set; }
		public string? DepartmentName { get; set; }
	}

	public class StudentMiniVm
	{
		public string StudentId { get; set; } = ""; // StudentProfile.Id
		public string UserId { get; set; } = "";
		public string? StudentCode { get; set; }
		public string? DepartmentId { get; set; }
		public string? DepartmentName { get; set; }
		public string? CurrentSemester { get; set; }
	}

	// =========================
	// AGENDA
	// =========================
	public class EventAgendaVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string SessionName { get; set; } = "";
		public string? Description { get; set; }
		public string? SpeakerInfo { get; set; }

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public string? Location { get; set; } // agenda location text
	}

	// =========================
	// DOCUMENT
	// =========================
	public class EventDocumentVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string FileName { get; set; } = "";
		public string Url { get; set; } = "";
		public string? Type { get; set; }
	}

	// =========================
	// TICKET + CHECKIN
	// =========================
	public class TicketVm
	{
		public string Id { get; set; } = "";
		public string TicketCode { get; set; } = "";
		public string EventId { get; set; } = "";

		public string StudentId { get; set; } = "";
		public StudentMiniVm? Student { get; set; }

		public TicketStatusEnum Status { get; set; }  // nếu Ticket.Status là string
		public DateTime RegisteredAt { get; set; }
		public DateTime? CheckInTime { get; set; }
	}

	public class CheckInHistoryVm
	{
		public string Id { get; set; } = "";
		public string TicketId { get; set; } = "";

		public DateTime ScannedAt { get; set; }
		public string? DeviceName { get; set; }
		public DateTime? ScanTime { get; set; }   // nếu bạn có cột ScanTime riêng
		public string? ScanType { get; set; }
		public string? Location { get; set; }
	}

	// =========================
	// WAITLIST
	// =========================


	// =========================
	// FEEDBACK
	// =========================
	public class FeedbackVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string StudentId { get; set; } = "";
		public StudentMiniVm? Student { get; set; }

		public int Rating { get; set; }
		public string? Comment { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	// =========================
	// QUIZ
	// =========================
	public class QuizDetailVm
	{
		public string QuizId { get; set; } = "";
		public string EventId { get; set; } = "";

		public string Title { get; set; } = "";
		public string Type { get; set; } = "";    // EventQuiz.Type
		public bool IsActive { get; set; }
		public int PassingScore { get; set; }
		public DateTime CreatedAt { get; set; }

		public List<QuizQuestionVm> Questions { get; set; } = new();
		public List<StudentQuizScoreVm> Scores { get; set; } = new(); // Organizer/Admin
	}

	public class QuizQuestionVm
	{
		public string Id { get; set; } = "";
		public string QuizId { get; set; } = "";

		public string QuestionText { get; set; } = "";
		public string? OptionA { get; set; }
		public string? OptionB { get; set; }
		public string? OptionC { get; set; }
		public string? OptionD { get; set; }

		public string? CorrectAnswer { get; set; } // A/B/C/D (tùy bạn lưu)
		public int ScorePoint { get; set; }
	}

	public class StudentQuizScoreVm
	{
		public string Id { get; set; } = "";
		public string QuizId { get; set; } = "";

		public string StudentId { get; set; } = "";
		public StudentMiniVm? Student { get; set; }

		public int TotalScore { get; set; }
		public bool IsPassed { get; set; }
		public DateTime SubmittedAt { get; set; }
	}

	// =========================
	// APPROVAL LOG
	// =========================
	public class ApprovalLogVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string ApprovedBy { get; set; } = ""; // user/staff id
		public string Action { get; set; } = "";     // Send/Approve/Reject/Publish/Cancel...
		public string? Comment { get; set; }
		public DateTime LogDate { get; set; }
	}

	// =========================
	// BUDGET + RECEIPT
	// =========================
	public class BudgetProposalVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string Title { get; set; } = "";
		public string? Description { get; set; }
		public decimal Amount { get; set; }
		public decimal PlannedAmount { get; set; } // nếu bạn dùng field này
	//	public string? Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class ExpenseReceiptVm
	{
		public string Id { get; set; } = "";
		public string BudgetProposalId { get; set; } = "";

		public string Title { get; set; } = "";
		public decimal Amount { get; set; }
		public string? ReceiptImageUrl { get; set; }

		public string? SubmittedBy { get; set; }
		public DateTime? SubmittedAt { get; set; }
		public string? Status { get; set; }
	}

	// =========================
	// TEAM
	// =========================
	public class EventTeamVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public string TeamName { get; set; } = "";
		public string? Description { get; set; }
		public decimal Score { get; set; }
		public int? PlaceRank { get; set; }
		public DateTime CreatedAt { get; set; }
		public List<TeamMemberVm> TeamMembers { get; set; } = new();
	}

	public class TeamMemberVm
	{
		public string Id { get; set; } = "";
		public string TeamId { get; set; } = "";
		public string? StudentId { get; set; }
		public string? StaffId { get; set; }
		public string MemberName { get; set; } = "";
		public string MemberEmail { get; set; } = "";
		public string RoleName { get; set; } = "";
		public string TeamRole { get; set; } = ""; // e.g. Leader, Member
	}

	// =========================
	// REMINDER + NOTIFICATION
	// =========================
	public class EventReminderVm
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";

		public int RemindBefore { get; set; }       // phút/giờ tuỳ bạn define
		public string? MessageTemplate { get; set; }
		public bool IsSent { get; set; }
	}

	public class NotificationVm
	{
		public string Id { get; set; } = "";
		public string UserId { get; set; } = "";

		public string Title { get; set; } = "";
		public string? Message { get; set; }
		public bool IsRead { get; set; }
		public string? Type { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}