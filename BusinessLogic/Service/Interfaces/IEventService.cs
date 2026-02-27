using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Interfaces
{
    // Interface focused on Event-related use-cases (SRP)
    public interface IEventService
    {
        Task CreateEventAsync(string userId, CreateEventRequestDto dto);
        Task UpdateEventAsync(string userId, string eventId, UpdateEventRequestDto dto);
        Task DeleteEventAsync(string userId, string eventId);

        Task SendForApprovalAsync(string userId, string eventId);

        Task<EventDetailsDto> GetEventDetailsAsync(string eventId, string? userId = null);

        // Return full list (no filters)
        Task<List<EventListDto>> GetMyEventsAsync(string userId);

        // Paged + filtered variant used by UI
        Task<BusinessLogic.DTOs.Role.Organizer.PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, string? status, string? semesterId, int page = 1, int pageSize = 10);
    }
}
