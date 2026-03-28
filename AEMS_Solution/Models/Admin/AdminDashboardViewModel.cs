using System.Collections.Generic;
using DataAccess.Entities;

namespace AEMS_Solution.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalStaff { get; set; }
        public int TotalErrorsToday { get; set; }
        public int TotalErrorsLast30Days { get; set; }

        // Chart Data
        public List<int> ErrorTrendData { get; set; } = new List<int>();
        public List<string> ErrorTrendLabels { get; set; } = new List<string>();

        // Demographics
        public Dictionary<string, int> UserDistribution { get; set; } = new Dictionary<string, int>();

        // New Insights
        public List<UserActivityLog> RecentActivities { get; set; } = new List<UserActivityLog>();
        public List<Notification> RecentNotifications { get; set; } = new List<Notification>();
    }
}
