using System.Collections.Generic;

namespace AEMS_Solution.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalStaff { get; set; }
        public int TotalErrorsToday { get; set; }

        // Chart Data
        public List<int> ErrorTrendData { get; set; } = new List<int>();
        public List<string> ErrorTrendLabels { get; set; } = new List<string>();

        // Demographics
        public Dictionary<string, int> UserDistribution { get; set; } = new Dictionary<string, int>();
    }
}
