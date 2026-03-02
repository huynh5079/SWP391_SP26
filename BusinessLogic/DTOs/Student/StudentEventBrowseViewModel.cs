using System.Collections.Generic;

namespace BusinessLogic.DTOs.Student
{
    public class StudentEventBrowseViewModel
    {
        public StudentDashboardStatsDto Stats { get; set; } = new();
        public List<StudentEventBrowseDto> Events { get; set; } = new();
    }
}
