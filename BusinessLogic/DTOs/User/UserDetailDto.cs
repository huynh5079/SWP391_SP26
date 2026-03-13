using DataAccess.Enum;

namespace BusinessLogic.DTOs.User
{
    public class UserDetailDto : UserListDto
    {
        public string? Phone { get; set; }
        public string? GoogleId { get; set; }
        
        // Specific Profile Info
        public string? StudentCode { get; set; }
        public string? StaffCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? CurrentSemester { get; set; } // Student
        public string? Position { get; set; } // Staff
        public bool HasPassword { get; set; }
    }
}
