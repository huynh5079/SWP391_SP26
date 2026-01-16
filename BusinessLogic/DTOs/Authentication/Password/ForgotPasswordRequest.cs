using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Authentication.Password
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Yêu cầu nhập địa chỉ email và phải đúng cấu trúc"), EmailAddress]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Yêu cầu nhập mật khẩu phải tối thiểu phải từ 8 kí tự trở lên"), MinLength(8)]
        public string NewPassword { get; set; } = default!;
    }
}
