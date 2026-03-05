using DataAccess.Enum;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.System
{
    public enum BroadcastTargetGroup
    {
        AllSystem,
        ByRole,
        SpecificEmail
    }

    public class BroadcastNotificationDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 kí tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [MaxLength(500, ErrorMessage = "Nội dung không được vượt quá 500 kí tự")]
        public string Message { get; set; } = string.Empty;

        [Required]
        public BroadcastTargetGroup TargetGroup { get; set; } = BroadcastTargetGroup.AllSystem;

        // Binds only if TargetGroup = ByRole
        public RoleEnum? TargetRole { get; set; }

        // Binds only if TargetGroup = SpecificEmail
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string? SpecificEmail { get; set; }
    }
}
