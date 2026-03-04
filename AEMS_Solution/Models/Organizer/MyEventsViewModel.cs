using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Organizer
{
    public class MyEventsViewModel
    {
        public List<OrganizerEventCardVm> Events { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public string? Search { get; set; }
        public EventStatusEnum Status { get; set; } //???????????????????????????????
        public ApprovalActionEnum StatusActionApprover { get; set; }
		public string? SemesterId { get; set; }
        
        public DateTime? DateFrom { get; set; }// thêm vào để làm filter tìm kiếm theo ngày
        public DateTime? DateTo { get; set; }
        public string? Location { get; set; }
        public string? Department { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    }


}
