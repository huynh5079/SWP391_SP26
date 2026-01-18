using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Otp
{
    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Yêu cầu nhập địa chỉ email và phải đúng cấu trúc"), EmailAddress]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Yêu cầu nhập mã OTP nếu muốn xác nhận!")]
        public string Code { get; set; } = default!;
    }
}
