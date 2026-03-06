using System.ComponentModel.DataAnnotations;

namespace AEMS_Solution.Models.Authentication
{
    public class RegisterOrganizerViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        public string Password { get; set; } = default!;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = default!;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = default!;

        [Required(ErrorMessage = "Mã số nhân viên/Mã ban tổ chức không được để trống")]
        public string StaffCode { get; set; } = default!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = default!;

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        public string Position { get; set; } = default!;
    }
}
