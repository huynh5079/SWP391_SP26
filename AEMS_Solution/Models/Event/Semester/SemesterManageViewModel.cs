using DataAccess.Enum;

namespace AEMS_Solution.Models.Event.Semester
{
    public class ApproverSemesterViewModel
    {
        public string PageTitle { get; set; } = "All Semesters";
        public string PageDescription { get; set; } = "Approver có thể xem danh sách học kỳ trong hệ thống.";
        public string? Search { get; set; }
        public string? Status { get; set; }
        public bool CanCreateNextSemester { get; set; }
        public List<SemesterItemViewModel> Semesters { get; set; } = new();
    }

    public class SemesterItemViewModel
    {
        public string SemesterId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SemesterStatusEnum Status { get; set; }
        public int EventCount { get; set; }
    }
}
