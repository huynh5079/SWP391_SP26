using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Approver
{
    public class PendingApprovalsViewModel
    {
        public List<ApproverEventCardVm> Events { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
    }

    public class ApproverDashboardStatsViewModel
    {
        public int TotalEventsPending { get; set; }
        public int TotalEventsApproved { get; set; }
        public int TotalEventsRejected { get; set; }
        public int EventsAwaitingAction { get; set; } // Ví dụ: Cùng = Pending
        
        public List<ApproverEventCardVm> RecentPendingEvents { get; set; } = new();
    }
    public class AgendaVm
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Speaker { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
    }

    public class DocumentVm
    {
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string? Type { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public class ApproverEventCardVm
    {
        public string EventId { get; set; } = "";
        public string Title { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EventStatusEnum Status { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Location { get; set; }
        public string? LastApprovalComment { get; set; }
    }

    public class ApproverEventDetailVm
    {
        public string EventId { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxCapacity { get; set; }
        public EventStatusEnum Status { get; set; }

        public string OrganizerName { get; set; } = "";
        public string OrganizerEmail { get; set; } = "";
        public string? Location { get; set; }
        public List<AgendaVm> Agendas { get; set; } = new();
        public List<DocumentVm> Documents { get; set; } = new();
        public List<ApprovalLogVm> ApprovalLogs { get; set; } = new();
    }

    public class ApprovalLogVm
    {
        public string ApproverId { get; set; } = "";
        public DataAccess.Enum.ApprovalActionEnum Action { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApproverActionFormVm
    {
        public string EventId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty; // approve | reject | requestchange
        public string? EventTitle { get; set; }
        public string? Heading { get; set; }
    }
}
