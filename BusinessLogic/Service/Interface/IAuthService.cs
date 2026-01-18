using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
using BusinessLogic.DTOs.Authentication.Register;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Interface
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
        //Task RegisterStudentAsync(RegisterStudentRequest dto);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<(bool ok, string message)> ChangePasswordAsync(string userId, ChangePasswordRequest req);
        // Forgot password (sau khi OTP reset đã verify)
        Task<(bool ok, string message)> ResetPasswordAsync(ForgotPasswordRequest req);
    }
}
