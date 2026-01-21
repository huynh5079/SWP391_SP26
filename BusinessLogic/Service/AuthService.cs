using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
using BusinessLogic.DTOs.Authentication.Register;
using BusinessLogic.Helper;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DateTimeHelper = BusinessLogic.Helper.DateTimeHelper;

namespace BusinessLogic.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;

        public AuthService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _uow.Users.FindByEmailAsync(dto.Email);
            if (user == null || user.Status == UserStatusEnum.Banned)
            {
                throw new Exception("Email hoặc mật khẩu không chính xác hoặc tài khoản bị khóa.");
            }

            if (!HashPasswordHelper.VerifyPassword(dto.Password, user.PasswordHash!))
            {
                throw new Exception("Email hoặc mật khẩu không chính xác.");
            }

            // Return Login Response
            return new LoginResponseDto
            {
                AccessToken = "DUMMY_TOKEN_FOR_COOKIE_AUTH", // In MVC with Cookie, this might not be used directly in response body but for consistency
                RefreshToken = "DUMMY_REFRESH_TOKEN",
                User = new LoggedInUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role.RoleName.ToString()!,
                    FullName = "User", // This should be fetched from Profile ideally, but for now simple User
                    AvatarUrl = user.AvatarUrl
                }
            };
        }

        public async Task RegisterStudentAsync(RegisterStudentRequest dto)
        {
            if (await _uow.Users.ExistsByEmailAsync(dto.Email))
            {
                throw new Exception("Email đã tồn tại trong hệ thống.");
            }

            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                // 1. Get Role
                var role = await _uow.Roles.GetAsync(r => r.RoleName == RoleEnum.Student);
                if (role == null) throw new Exception("System Error: Student Role not found.");

                // 2. Create User
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = HashPasswordHelper.HashPassword(dto.Password),
                    Phone = dto.Phone,
                    RoleId = role.Id,
                    Status = UserStatusEnum.Active,
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    UpdatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _uow.Users.CreateAsync(user);

                // 3. Create Student Profile
                var profile = new StudentProfile
                {
                    UserId = user.Id,
                    FullName = dto.FullName,
                    StudentCode = dto.StudentCode,
                    // Major = dto.Major, 
                    // Gender = dto.Gender,
                    // Note: Mapping extra fields if Entity supports them.
                    // Assuming current Entity structure.
                    // CreatedAt = ... inherited from BaseEntity? No, BaseEntity handles it in constructor usually but let's be explicit if needed.
                    // BaseEntity constructor sets CreatedAt = VietnamTime. So we might not need to set it manually if we use new().
                };
                
                await _uow.StudentProfiles.CreateAsync(profile);
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RegisterStaffAsync(RegisterStaffRequest dto)
        {
             if (await _uow.Users.ExistsByEmailAsync(dto.Email))
            {
                throw new Exception("Email đã tồn tại trong hệ thống.");
            }

            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                // 1. Get Role (Organizer/Approver share Staff Role)
                var role = await _uow.Roles.GetAsync(r => r.RoleName == RoleEnum.Staff);
                if (role == null) throw new Exception("System Error: Staff Role not found.");

                // 2. Create User
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = HashPasswordHelper.HashPassword(dto.Password),
                    Phone = dto.Phone,
                    RoleId = role.Id,
                    Status = UserStatusEnum.Active, // Or Pending if Staff needs approval? Lets assume Active for now.
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    UpdatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _uow.Users.CreateAsync(user);

                // 3. Create Staff Profile
                var profile = new StaffProfile
                {
                    UserId = user.Id,
                    FullName = dto.FullName,
                    StaffCode = dto.StaffCode,
                    Position = dto.Position
                };

                await _uow.StaffProfiles.CreateAsync(profile);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            return !await _uow.Users.ExistsByEmailAsync(email);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest req)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (!HashPasswordHelper.VerifyPassword(req.OldPassword, user.PasswordHash!))
            {
                throw new Exception("Mật khẩu cũ không chính xác");
            }

            user.PasswordHash = HashPasswordHelper.HashPassword(req.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        public async Task ResetPasswordAsync(ForgotPasswordRequest req)
        {
            // Logic to send email/reset.
        }
    }
}
