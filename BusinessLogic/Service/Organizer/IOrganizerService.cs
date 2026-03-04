using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;
namespace BusinessLogic.Service.Organizer
{
	public interface IOrganizerService
	{
		// Dashboard
		Task<OrganizerDto> GetDashboardAsync(string userId);

		// Counters
		Task<int> GetTotalEventAsync(string userId);
		Task<int> GetUpcomingEventAsync(string userId);
		Task<int> GetDraftEventAsync(string userId);

		// CRUD
		Task CreateEventAsync(string userId, CreateEventRequestDto dto);
		Task UpdateEventAsync(string userId, string eventId, UpdateEventRequestDto dto);
		Task DeleteEventAsync(string userId, string eventId);
        Task SoftDeleteEventAsync(string userId, string eventId);
        Task RestoreEventAsync(string userId, string eventId);

		// Paged deleted events (soft-deleted)
		Task<PagedResult<EventListDto>> GetMyDeletedEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10);

		// List events for organizer (paged)
		Task<PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10);

		// List events for organizer (simple list)
		Task<List<EventListDto>> GetMyEventsAsync(string userId);

		// Additional helpers used by controller/service
		Task SendForApprovalAsync(string userId, string eventId);

		Task<EventDetailsDto> GetEventDetailsAsync(string eventId, string? userId = null);

		Task<CreateEventDropdownsDto> GetCreateEventDropdownsAsync();
	}
}