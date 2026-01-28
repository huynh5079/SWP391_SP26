using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
using BusinessLogic.DTOs.Authentication.Register;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Auth
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
        Task RegisterStudentAsync(RegisterStudentRequestDto dto);
        Task RegisterStaffAsync(RegisterStaffRequestDto dto);
        Task<bool> IsEmailAvailableAsync(string email);
        Task ChangePasswordAsync(string userId, ChangePasswordRequestDto req);
        Task ForgotPasswordAsync(ForgotPasswordRequestDto req);
        Task ResetPasswordAsync(ResetPasswordRequestDto req);
        Task<DataAccess.Entities.User> LoginGoogleAsync(string email, string googleId, string fullName, string avatarUrl);

    }
}
