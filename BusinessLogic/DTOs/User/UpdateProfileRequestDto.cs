using Microsoft.AspNetCore.Http;

namespace BusinessLogic.DTOs.User
{
    public class UpdateProfileRequestDto
    {
        // Common
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; } // For future upload support

        // Student Specific
        public string? StudentCode { get; set; }
        public string? CurrentSemester { get; set; }

        // Staff Specific
        public string? StaffCode { get; set; }
        public string? Position { get; set; }
    }
}
