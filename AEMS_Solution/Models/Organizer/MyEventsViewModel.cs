using System;
using System.Collections.Generic;

namespace AEMS_Solution.Models.Organizer
{
    public class MyEventsViewModel
    {
        public List<OrganizerEventCardVm> Events { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? SemesterId { get; set; }


        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    }


}
