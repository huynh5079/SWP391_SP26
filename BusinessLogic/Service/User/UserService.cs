using BusinessLogic.DTOs.User;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.User
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PaginationResult<UserListDto>> GetUsersAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null, 
            string? role = null, 
            UserStatusEnum? status = null)
        {
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
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            return new PaginationResult<UserListDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<UserDetailDto?> GetUserDetailAsync(string id)
        {
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

            await _uow.Users.UpdateAsync(user);
            return true;
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequestDto request)
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
            
            // Allow updating avatar URL if provided (client upload handled in Controller)
            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            // 2. Update Specific Role Fields
            if (user.Role.RoleName == RoleEnum.Student)
            {
                if (user.StudentProfile == null)
                {
                    // Should theoretically exist if role is correct, but safe to check or create
                     // user.StudentProfile = new StudentProfile { UserId = userId }; // If creating on fly needed
                }
                
                if (user.StudentProfile != null)
                {
                   // Note: StudentCode might be read-only business-wise, but allowing edit if requested
                   if(!string.IsNullOrEmpty(request.StudentCode)) user.StudentProfile.StudentCode = request.StudentCode;
                   if(!string.IsNullOrEmpty(request.CurrentSemester)) user.StudentProfile.CurrentSemester = request.CurrentSemester;
                }
            }
            else if (user.Role.RoleName == RoleEnum.Organizer || user.Role.RoleName == RoleEnum.Approver || user.Role.RoleName == RoleEnum.Admin)
            {
                if (user.StaffProfile != null)
                {
                     if(!string.IsNullOrEmpty(request.StaffCode)) user.StaffProfile.StaffCode = request.StaffCode;
                     if(!string.IsNullOrEmpty(request.Position)) user.StaffProfile.Position = request.Position;
                }
            }

            await _uow.Users.UpdateAsync(user);
            return true;
        }
    }
}
