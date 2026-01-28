using BusinessLogic.DTOs;
using BusinessLogic.DTOs.User;
using DataAccess.Enum;
using DataAccess.Entities;

namespace BusinessLogic.Service.Interface
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

        Task<bool> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    }
}
