using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.Service.Event
{
    // Interface focused on Event-related use-cases (SRP)
    public interface IEventService
    {
        Task<string> CreateEventAsync(string userId, CreateEventRequestDto dto);
        Task UpdateEventAsync(string userId, string eventId, UpdateEventRequestDto dto);
        Task DeleteEventAsync(string userId, string eventId);

        Task SendForApprovalAsync(string userId, string eventId);
        Task CancelEventAsync(string userId, string eventId);
        Task PublishEventAsync(string userId, string eventId);

        Task<EventDetailsDto> GetEventDetailsAsync(string eventId, string? userId = null);

        // Return full list (no filters)
        Task<List<EventListDto>> GetMyEventsAsync(string userId);

        // Paged + filtered variant used by UI
        Task<PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10);
		Task SoftDeleteEventAsync(string userId, string eventId);
		Task RestoreEventAsync(string userId, string eventId);

		// Paged deleted events (soft-deleted)
		Task<PagedResult<EventListDto>> GetMyDeletedEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10);
	}
}
