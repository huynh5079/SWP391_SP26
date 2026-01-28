using DataAccess.Enum;

namespace BusinessLogic.DTOs.User
{
    public class UserManagementDto
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public UserStatusEnum? Status { get; set; }
        public string? AvatarUrl { get; set; }
        public bool? IsBanned { get; set; }
    }
}
