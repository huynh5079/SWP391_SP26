using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.User;
using BusinessLogic.Service.System;
using BusinessLogic.Service.ValiDateRole.ValiDateforAdmin.LockAndUnlockLimit;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.User
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly ILockAndUnlockLimitValidator _lockAndUnlockLimitValidator;
        private readonly BusinessLogic.Storage.IFileStorageService _fileStorageService;
        private readonly ILogger<UserService> _logger;
        private readonly ISystemErrorLogService _systemErrorLogService;

        public UserService(
            IUnitOfWork uow,
            INotificationService notificationService,
            ILockAndUnlockLimitValidator lockAndUnlockLimitValidator,
            BusinessLogic.Storage.IFileStorageService fileStorageService,
            ILogger<UserService> logger,
            ISystemErrorLogService systemErrorLogService)
        {
            _uow = uow;
            _notificationService = notificationService;
            _lockAndUnlockLimitValidator = lockAndUnlockLimitValidator;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _systemErrorLogService = systemErrorLogService;
        }

        public async Task<PaginationResult<UserListDto>> GetUsersAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null, 
            string? role = null, 
            UserStatusEnum? status = null)
        {
            await ReactivateExpiredUsersAsync();

            // Parse role string to Enum
            RoleEnum? roleEnum = null;
            if (!string.IsNullOrEmpty(role) && Enum.TryParse<RoleEnum>(role, true, out var parsedRole))
            {
                roleEnum = parsedRole;
            }

            // Build predicate
            Expression<Func<UserEntity, bool>> predicate = u => 
                (string.IsNullOrEmpty(searchTerm) || u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm)) &&
                (!roleEnum.HasValue || u.Role.RoleName == roleEnum) &&
                (!status.HasValue || u.Status == status);

            // Include Role
            Func<IQueryable<UserEntity>, IQueryable<UserEntity>> includes = q => q.Include(u => u.Role);

            var allUsers = await _uow.Users.GetAllAsync(predicate, includes);
            
            var totalCount = allUsers.Count();
            var items = allUsers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.Role.RoleName.ToString()!,
                    Status = u.Status,
                    AvatarUrl = u.AvatarUrl,
                    IsBanned = u.IsBanned,
                    ReactivateAt = u.ReactivateAt,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            return new PaginationResult<UserListDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<UserDetailDto?> GetUserDetailAsync(string id)
        {
            await ReactivateExpiredUsersAsync(id);

            Func<IQueryable<UserEntity>, IQueryable<UserEntity>> includes = q => q
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .ThenInclude(p => p.Department)
                .Include(u => u.StaffProfile)
                .ThenInclude(p => p.Department);

            var user = await _uow.Users.GetAsync(u => u.Id == id, includes);

            if (user == null) return null;

            var dto = new UserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.Role.RoleName.ToString()!,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                IsBanned = user.IsBanned,
                ReactivateAt = user.ReactivateAt,
                GoogleId = user.GoogleId,
                CreatedAt = user.CreatedAt,
                HasPassword = !string.IsNullOrEmpty(user.PasswordHash)
            };

            // Map specific profile data
            if (user.Role.RoleName == RoleEnum.Student && user.StudentProfile != null)
            {
                dto.StudentCode = user.StudentProfile.StudentCode;
                dto.DepartmentName = user.StudentProfile.Department?.Name;
                dto.CurrentSemester = user.StudentProfile.CurrentSemester;
            }
            else if ((user.Role.RoleName == RoleEnum.Organizer || user.Role.RoleName == RoleEnum.Approver || user.Role.RoleName == RoleEnum.Admin) && user.StaffProfile != null)
            {
                dto.StaffCode = user.StaffProfile.StaffCode;
                dto.DepartmentName = user.StaffProfile.Department?.Name;
                dto.Position = user.StaffProfile.Position;
            }

            return dto;
        }

        public async Task<UserDetailDto?> GetMyProfileAsync(string id)
        {
            return await GetUserDetailAsync(id);
        }

        public async Task<bool> ToggleBanUserAsync(string id)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return false;

            // Toggle IsBanned
            user.IsBanned = !user.IsBanned.GetValueOrDefault();
            user.ReactivateAt = null;

            // Update Status based on IsBanned
            if (user.IsBanned == true)
            {
                user.Status = UserStatusEnum.Inactive; 
            }
            else
            {
                // Only set back to Active if logic permits, usually yes for unban
                user.Status = UserStatusEnum.Active;
            }

            await _uow.SaveChangesAsync();

            // Send Realtime Notification
            string notifyTitle = user.IsBanned == true ? "Tài khoản bị cấm" : "Tài khoản được mở khóa";
            string notifyMsg = user.IsBanned == true 
                ? "Tài khoản của bạn đã bị quản trị viên khóa vô thời hạn." 
                : "Tài khoản của bạn đã được quản trị viên mở khóa trở lại.";
            DataAccess.Enum.NotificationType typeEnum = user.IsBanned == true 
                ? DataAccess.Enum.NotificationType.AccountBan 
                : DataAccess.Enum.NotificationType.AccountUnban;

            await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
            {
                ReceiverId = id,
                Title = notifyTitle,
                Message = notifyMsg,
                Type = typeEnum,
                RelatedEntityId = id // User ID
            });

            return true;
        }

        public async Task<bool> SetUserLockAsync(AdminSoftDeleteLimitDTO request)
        {
            var user = await _lockAndUnlockLimitValidator.ValidateSetUserLockAsync(request);

            user.Status = UserStatusEnum.Inactive;
            user.IsBanned = true;
            user.ReactivateAt = request.ReactivateAt;

            await _uow.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
            {
                ReceiverId = user.Id,
                Title = "Tài khoản bị khóa",
                Message = request.ReactivateAt.HasValue
                    ? $"Tài khoản của bạn đã bị quản trị viên khóa đến {request.ReactivateAt.Value:dd/MM/yyyy HH:mm}."
                    : "Tài khoản của bạn đã bị quản trị viên khóa cho đến khi có thông báo mới.",
                Type = DataAccess.Enum.NotificationType.AccountBan,
                RelatedEntityId = user.Id
            });

            return true;
        }

        public async Task<int> ReactivateExpiredUsersAsync(string? userId = null)
        {
            var now = DateTimeHelper.GetVietnamTime();
			const int batchSize = 200;
			var totalUnlocked = 0;

			while (true)
			{
				var expiredUsers = (await _uow.Users.GetAllAsync(
					u => u.IsBanned == true
						&& u.Status == UserStatusEnum.Inactive
						&& u.ReactivateAt.HasValue
						&& u.ReactivateAt <= now
						&& (string.IsNullOrEmpty(userId) || u.Id == userId),
					q => q.OrderBy(u => u.ReactivateAt).Take(batchSize)))
					.ToList();

				if (expiredUsers.Count == 0)
				{
					break;
				}

				foreach (var expiredUser in expiredUsers)
				{
					expiredUser.Status = UserStatusEnum.Active;
					expiredUser.IsBanned = false;
					expiredUser.ReactivateAt = null;
				}

				await _uow.SaveChangesAsync();

				foreach (var expiredUser in expiredUsers)
				{
					await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
					{
						ReceiverId = expiredUser.Id,
						Title = "Tài khoản được mở khóa",
						Message = "Tài khoản của bạn đã được tự động mở khóa theo thời gian đã thiết lập.",
						Type = DataAccess.Enum.NotificationType.AccountUnban,
						RelatedEntityId = expiredUser.Id
					});
				}

				totalUnlocked += expiredUsers.Count;

				if (!string.IsNullOrEmpty(userId))
				{
					break;
				}
			}

			return totalUnlocked;
        }

        public async Task<string?> UpdateAvatarAsync(string userId, IFormFile file)
        {
            try
            {
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null) return null;

                if (file != null && file.Length > 0)
                {
                    _logger.LogInformation("[UpdateAvatarAsync] Received file: {FileName}, Length: {Length}", file.FileName, file.Length);
                    var uploadResult = await _fileStorageService.UploadSingleAsync(
                        file,
                        DataAccess.Enum.UploadContext.Avatar,
                        userId);

                    if (uploadResult != null)
                    {
                        _logger.LogInformation("[UpdateAvatarAsync] Upload success: {Url}", uploadResult.Url);
                        user.AvatarUrl = uploadResult.Url;
                        
                        await _uow.Users.UpdateAsync(user);
                        await _uow.SaveChangesAsync();
                        
                        return uploadResult.Url;
                    }
                    else
                    {
                        _logger.LogWarning("[UpdateAvatarAsync] UploadResult was null");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateAvatarAsync] Error updating avatar for user {UserId}", userId);
                await _systemErrorLogService.LogErrorAsync(ex, userId, "UserService.UpdateAvatarAsync");
                throw;
            }
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequestDto request)
        {
            try 
            {
                Func<IQueryable<UserEntity>, IQueryable<UserEntity>> includes = q => q
                  .Include(u => u.Role)
                  .Include(u => u.StudentProfile)
                  .Include(u => u.StaffProfile);

                var user = await _uow.Users.GetAsync(u => u.Id == userId, includes);
                if (user == null) return false;

                // 1. Update Common User Fields
                user.FullName = request.FullName;
                user.Phone = request.Phone;
                
                // Allow updating avatar URL if provided
                if (!string.IsNullOrEmpty(request.AvatarUrl))
                {
                    user.AvatarUrl = request.AvatarUrl;
                }

                // 2. Update Specific Role Fields
                if (user.Role?.RoleName == RoleEnum.Student)
                {
                    if (user.StudentProfile != null)
                    {
                        if (!string.IsNullOrEmpty(request.StudentCode)) user.StudentProfile.StudentCode = request.StudentCode;
                        if (!string.IsNullOrEmpty(request.CurrentSemester)) user.StudentProfile.CurrentSemester = request.CurrentSemester;
                        if (!string.IsNullOrEmpty(request.DepartmentId)) user.StudentProfile.DepartmentId = request.DepartmentId;
                        user.StudentProfile.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    }
                }
                else if (user.Role?.RoleName == RoleEnum.Organizer || user.Role?.RoleName == RoleEnum.Approver || user.Role?.RoleName == RoleEnum.Admin)
                {
                    if (user.StaffProfile != null)
                    {
                        if (!string.IsNullOrEmpty(request.StaffCode)) user.StaffProfile.StaffCode = request.StaffCode;
                        if (!string.IsNullOrEmpty(request.Position)) user.StaffProfile.Position = request.Position;
                        if (!string.IsNullOrEmpty(request.DepartmentId)) user.StaffProfile.DepartmentId = request.DepartmentId;
                        user.StaffProfile.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    }
                }

                await _uow.Users.UpdateAsync(user);
                await _uow.SaveChangesAsync();
                
                _logger.LogInformation("[UpdateProfileAsync] Changes saved for user: {Email}. Final AvatarUrl: {AvatarUrl}", user.Email, user.AvatarUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateProfileAsync] Error updating profile for user {UserId}", userId);
                await _systemErrorLogService.LogErrorAsync(ex, userId, "UserService.UpdateProfileAsync");
                throw;
            }
        }
    }
}
