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
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
        Task RegisterStudentAsync(RegisterStudentRequest dto);
        Task RegisterStaffAsync(RegisterStaffRequest dto);
        Task<DataAccess.Entities.User> LoginGoogleAsync(string email, string googleId, string fullName, string avatarUrl);
        Task<bool> IsEmailAvailableAsync(string email);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest req);
        // Forgot password (simplified for now)
        Task ResetPasswordAsync(ForgotPasswordRequest req);
    }
}
