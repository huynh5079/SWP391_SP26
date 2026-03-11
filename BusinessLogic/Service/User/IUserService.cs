using BusinessLogic.DTOs;
using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.User;
using DataAccess.Enum;
using DataAccess.Entities;

namespace BusinessLogic.Service.User
{
    public interface IUserService
    {
        Task<PaginationResult<UserListDto>> GetUsersAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null, 
            string? role = null, 
            UserStatusEnum? status = null);

        Task<UserDetailDto?> GetUserDetailAsync(string id);
        
        Task<UserDetailDto?> GetMyProfileAsync(string id);

        Task<bool> ToggleBanUserAsync(string id);

        Task<bool> SetUserLockAsync(AdminSoftDeleteLimitDTO request);

        Task<int> ReactivateExpiredUsersAsync(string? userId = null);

        Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequestDto request);
    }
}
