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
    }
}
