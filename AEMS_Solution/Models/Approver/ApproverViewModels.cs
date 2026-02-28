using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Approver
{
    public class ApproverDashboardViewModel
    {
        public List<ApproverEventCardVm> Events { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
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
    }

    public class ApproverEventDetailVm
    {
        public string EventId { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxCapacity { get; set; }
        public EventStatusEnum Status { get; set; }

        public string OrganizerName { get; set; } = "";
        public string OrganizerEmail { get; set; } = "";

        public List<ApprovalLogVm> ApprovalLogs { get; set; } = new();
    }

    public class ApprovalLogVm
    {
        public string ApproverId { get; set; } = "";
        public DataAccess.Enum.ApprovalActionEnum Action { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
