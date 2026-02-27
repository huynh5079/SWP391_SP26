using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Dashboard
{
    public interface IDashboardService
    {
        Task<OrganizerDto> GetDashboardAsync(string userId);
        Task<int> GetTotalEventAsync(string userId);
        Task<int> GetUpcomingEventAsync(string userId);
        Task<int> GetDraftEventAsync(string userId);
    }
}