using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Organizer
{
	public class OrganizerDashboardViewModel
	{
		// ===== Organizer Info (StaffProfile + User) =====
		public string OrganizerId { get; set; } = "";      // StaffProfile.Id
		public string UserId { get; set; } = "";           // User.Id
		public string FullName { get; set; } = "";
		public string Email { get; set; } = "";
		public string? AvatarUrl { get; set; }

		public string? DepartmentId { get; set; }
		public string? DepartmentName { get; set; }

		// ===== Filters (optional) =====
		public string? SemesterId { get; set; }
		public string? StatusFilter { get; set; }          // Draft / Published / Ended...???????????????
		public string? Search { get; set; }                // search theo title
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }

		// ===== Quick Stats (aggregate object) =====
		public OrganizerDashboardStats Stats { get; set; } = new();

		// Shortcut properties (cho view dễ dùng) - KHÔNG TRÙNG TÊN VỚI LISTS
		public int TotalEvents => Stats?.TotalEvents ?? 0;
		public int UpcomingEventsCount => Stats?.UpcomingEvents ?? 0;
		public int DraftEventsCount => Stats?.DraftEvents ?? 0;

		// Những field view đang tham chiếu trực tiếp
		public int RegistrationsToday { get; set; } = 0;
		public decimal DepositCollectedThisMonth { get; set; } = 0m;

		// Charts / distribution used by the view scripts
		public List<string> RegistrationTrendLabels { get; set; } = new();
		public List<int> RegistrationTrendData { get; set; } = new();
		public Dictionary<string, int> EventStatusDistribution { get; set; } = new();

		// ===== Event Lists =====
		public List<OrganizerEventCardVm> UpcomingEvents { get; set; } = new();
		public List<OrganizerEventCardVm> DraftEvents { get; set; } = new();
		public List<OrganizerEventCardVm> RecentEvents { get; set; } = new();

		// ===== Recent feedback (cho organizer xem chất lượng event) =====
		public List<EventFeedbackSummaryVm> RecentFeedbacks { get; set; } = new();

		// ===== Dropdown Options (optional) =====
		public List<SimpleOptionVm> SemesterOptions { get; set; } = new();
		public List<SimpleOptionVm> DepartmentOptions { get; set; } = new();
	}

	public class OrganizerDashboardStats
	{
		// Event counts
		public int TotalEvents { get; set; }
		public int DraftEvents { get; set; }
		public int PublishedEvents { get; set; }
		public int UpcomingEvents { get; set; }
		public int OngoingEvents { get; set; }
		public int EndedEvents { get; set; }

		// Registration stats (Ticket / Waitlist)
		public int TotalRegistrations { get; set; }     // Ticket count
		public int TotalCheckedIn { get; set; }         // Ticket checked in
		public int TotalWaitlist { get; set; }          // EventWaitlist count

		// Feedback stats
		public double AvgRating { get; set; }
		public int FeedbackCount { get; set; }
	}

	// Card hiển thị event trên dashboard
	public class OrganizerEventCardVm
	{
		public string EventId { get; set; } = "";
		public string Title { get; set; } = "";
		public string? ThumbnailUrl { get; set; }
		public string? OrganizerName { get; set; }

		public string? SemesterId { get; set; }
		public string? SemesterName { get; set; }

		public string? DepartmentId { get; set; }
		public string? DepartmentName { get; set; }

		public string? Location { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public int MaxCapacity { get; set; }

		public EventStatusEnum Status { get; set; } // Draft/Published/...

        public ApprovalActionEnum ApprovalActionEnum { get; set; }  // Pending/Approved/Rejected/RequestChange
																   // Aggregations for UI
		public int RegisteredCount { get; set; }        // Ticket count
		public int CheckedInCount { get; set; }         // Ticket checked in
		public int WaitlistCount { get; set; }          // waitlist count
		public double AvgRating { get; set; }           // avg feedback

		public string? Mode { get; set; }        // Offline/Online/Hybrid
		public string? MeetingUrl { get; set; }
		public bool CanEdit { get; set; }
		public bool CanSendForApproval { get; set; }
		public bool CanPublish { get; set; }
		public bool CanCancel { get; set; }
		public bool IsOwnedByCurrentUser { get; set; }
		public bool IsPubliclyVisible { get; set; }

		// Aggregated Sentiment Data for Event Performance Chart
		public double TechnicalAvg { get; set; }
		public double ContentAvg { get; set; }
		public double InstructorAvg { get; set; }
		public double AssessmentAvg { get; set; }
		public double GeneralSentimentAvg { get; set; }
	}

	// Tóm tắt feedback gần đây
	public class EventFeedbackSummaryVm
	{
		public string EventId { get; set; } = "";
		public string EventTitle { get; set; } = "";
		public int Rating { get; set; }
		public string? Comment { get; set; }
		public DateTime? CreatedAt { get; set; }

		public string? StudentId { get; set; }
		public string? StudentCode { get; set; }
	}

	public class SimpleOptionVm
	{
		public string Id { get; set; } = "";
		public string Text { get; set; } = "";
	}
}
