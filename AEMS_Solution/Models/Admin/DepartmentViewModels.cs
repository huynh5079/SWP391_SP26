using System.ComponentModel.DataAnnotations;

namespace AEMS_Solution.Models.Admin
{
    public class DepartmentIndexViewModel
    {
        public List<DepartmentViewModel> Departments { get; set; } = new();
    }

    public class DepartmentViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int NumberOfStaff { get; set; } // Can be implemented later if needed
        public int NumberOfStudents { get; set; } // Can be implemented later if needed
    }

    public class DepartmentCreateViewModel
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã phòng ban là bắt buộc (Ví dụ: SE, SA)")]
        [MaxLength(50, ErrorMessage = "Mã phòng ban không được vượt quá 50 ký tự")]
        public string Code { get; set; } = string.Empty;
    }

    public class DepartmentEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã phòng ban là bắt buộc (Ví dụ: SE, SA)")]
        [MaxLength(50, ErrorMessage = "Mã phòng ban không được vượt quá 50 ký tự")]
        public string Code { get; set; } = string.Empty;
    }
}
