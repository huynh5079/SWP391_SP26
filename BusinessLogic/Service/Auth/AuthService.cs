using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Authentication.Password;
using BusinessLogic.DTOs.Authentication.Register;
using BusinessLogic.Helper;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;

        public AuthService(IUnitOfWork uow, IEmailService emailService)
        {
            _uow = uow;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _uow.Users.FindByEmailAsync(dto.Email);
            // Check if user exists, is banned via boolean flag, or has Banned/Inactive status
            if (user == null || user.IsBanned == true || user.Status == UserStatusEnum.Banned || user.Status == UserStatusEnum.Inactive)
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
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }

        public async Task RegisterStudentAsync(RegisterStudentRequestDto dto)
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
                var user = new UserEntity
                {
                    Email = dto.Email,
                    PasswordHash = HashPasswordHelper.HashPassword(dto.Password),
                    FullName = dto.FullName,
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

        public async Task RegisterStaffAsync(RegisterStaffRequestDto dto)
        {
             if (await _uow.Users.ExistsByEmailAsync(dto.Email))
            {
                throw new Exception("Email đã tồn tại trong hệ thống.");
            }

            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                // 1. Get Role (Organizer/Approver share Staff Role logic, defaulting to Organizer for now)
                var role = await _uow.Roles.GetAsync(r => r.RoleName == RoleEnum.Organizer);
                if (role == null) throw new Exception("System Error: Organizer Role not found in Database. Please check Seeding.");

                // 2. Create User
                var user = new UserEntity
                {
                    Email = dto.Email,
                    PasswordHash = HashPasswordHelper.HashPassword(dto.Password),
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    RoleId = role.Id,
                    Status = UserStatusEnum.Active, // Or Pending if Staff needs approval? Lets assume Active for now.
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    UpdatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _uow.Users.CreateAsync(user);

                // 3. Create Staff Profile (Note: Entity name is still StaffProfile, consider renaming in future phase)
                var profile = new StaffProfile
                {
                    UserId = user.Id,
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

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequestDto req)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            // Google users might not have a password hash
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                 throw new Exception("Tài khoản này đăng nhập bằng Google, vui lòng dùng chức năng 'Quên mật khẩu' để thiết lập mật khẩu mới.");
            }

            if (!HashPasswordHelper.VerifyPassword(req.OldPassword, user.PasswordHash))
            {
                throw new Exception("Mật khẩu cũ không chính xác");
            }

            user.PasswordHash = HashPasswordHelper.HashPassword(req.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        public async Task SetPasswordAsync(string userId, SetPasswordRequestDto req)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new Exception("Tài khoản đã có mật khẩu. Vui lòng sử dụng chức năng Đổi mật khẩu.");
            }

            user.PasswordHash = HashPasswordHelper.HashPassword(req.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        // Note: For MVP/Production without Redis, we might use a static dictionary or cache.
        // Ideally, this should be in a separate TokenService or Redis.
        private static readonly Dictionary<string, (string Email, DateTime Expiry)> _resetTokens = new();

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto req)
        {
            var user = await _uow.Users.FindByEmailAsync(req.Email);
            // Security: Always return success even if user not found to prevent enumeration
            if (user == null) return;

            // Generate Token (GUID for now)
            var token = Guid.NewGuid().ToString();
            
            // Store Token (Valid for 15 mins)
            lock (_resetTokens)
            {
                // Remove old tokens for this email if any (cleanup)
                var keysToRemove = _resetTokens.Where(x => x.Value.Email == req.Email).Select(x => x.Key).ToList();
                foreach (var key in keysToRemove) _resetTokens.Remove(key);

                _resetTokens[token] = (req.Email, DateTimeHelper.GetVietnamTime().AddMinutes(15));
            }

            // Send Email
            await _emailService.SendPasswordResetEmailAsync(req.Email, token);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto req)
        {
            string email;
            lock (_resetTokens)
            {
                if (!_resetTokens.ContainsKey(req.Token))
                {
                    throw new Exception("Mã xác thực không hợp lệ hoặc đã hết hạn.");
                }

                var data = _resetTokens[req.Token];
                if (data.Expiry < DateTimeHelper.GetVietnamTime())
                {
                   _resetTokens.Remove(req.Token);
                   throw new Exception("Mã xác thực đã hết hạn.");
                }
                
                email = data.Email;
                // Invalidate token immediately
                _resetTokens.Remove(req.Token);
            }

            var user = await _uow.Users.FindByEmailAsync(email);
            if (user == null) throw new Exception("User not found via token context (Data consistency error).");

            user.PasswordHash = HashPasswordHelper.HashPassword(req.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        public async Task<UserEntity> LoginGoogleAsync(string email, string googleId, string fullName, string avatarUrl)
        {
            var user = await _uow.Users.FindByEmailAsync(email);

            // Case A: User Exists
            if (user != null)
            {
                if (user.IsBanned == true || user.Status == UserStatusEnum.Banned || user.Status == UserStatusEnum.Inactive)
                {
                    throw new Exception("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                }

                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    if (!string.IsNullOrEmpty(avatarUrl) && string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        user.AvatarUrl = avatarUrl;
                    }
                    await _uow.Users.UpdateAsync(user);
                }
                return user;
            }

            // Case B: User does NOT Exist - Auto Register as Student
            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                var role = await _uow.Roles.GetAsync(r => r.RoleName == RoleEnum.Student);
                if (role == null) throw new Exception("System Error: Student Role not found.");

                var newUser = new UserEntity
                {
                    Email = email,
                    GoogleId = googleId,
                    FullName = fullName,
                    // UserName = email, // Removed: User entity does not have UserName property
                    AvatarUrl = avatarUrl,
                    RoleId = role.Id,
                    Status = UserStatusEnum.Active,
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    UpdatedAt = DateTimeHelper.GetVietnamTime(),
                    PasswordHash = null // No password for Google users initially
                };

                await _uow.Users.CreateAsync(newUser);

                // Start: Student Code Generation (Simple Logic)
                // Format: S + Year + Random 4 digits
                var year = DateTimeHelper.GetVietnamTime().Year;
                var random = new Random();
                var studentCode = $"S{year}{random.Next(1000, 9999)}"; 
                // Note: In production, check for collision
                
                var studentProfile = new StudentProfile
                {
                    UserId = newUser.Id,
                    StudentCode = studentCode,
                };

                await _uow.StudentProfiles.CreateAsync(studentProfile);
                await transaction.CommitAsync();

                return newUser;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
