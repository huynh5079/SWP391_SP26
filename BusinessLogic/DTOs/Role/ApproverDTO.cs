using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role
{
	public class ApprovalLogDto
	{
		public string EventId { get; set; } = "";
		public string ApproverId { get; set; } = "";
		public string ApproverName { get; set; } = "";
		public ApprovalActionEnum Action { get; set; }     // Approve/Reject/RequestChange
		public string? Comment { get; set; }
		public DateTime CreatedAt { get; set; }
	}
	public class ApproverEventDetailDto
	{
		// Event info
		public string EventId { get; set; } = "";
		public string Title { get; set; } = "";
		public string? Description { get; set; }

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public int MaxCapacity { get; set; }

		public string Status { get; set; } = ""; // Pending/Approved/Rejected/Published...

		// Organizer info (nếu cần)
		public string OrganizerId { get; set; } = "";
		public string OrganizerName { get; set; } = "";
		public string OrganizerEmail { get; set; } = "";

		// Logs
		public List<ApprovalLogDto> ApprovalLogs { get; set; } = new();
	}

	public class ApproverDto
	{
		public EventItemDto Event { get; set; } = new();
		public List<ApprovalLogDto> ApprovalLogs { get; set; } = new();
	}
}
