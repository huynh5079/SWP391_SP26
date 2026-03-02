using System;
using System.Collections.Generic;

namespace BusinessLogic.DTOs.Student
{
    public class StudentDashboardStatsDto
    {
        public int TotalRegistered { get; set; }
        public int UpcomingThisWeek { get; set; }
        public int PendingFeedbacks { get; set; }
    }
}
