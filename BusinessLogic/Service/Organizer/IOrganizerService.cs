using BusinessLogic.DTOs.Role.Organizer;
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
    
       
        // List events for organizer
        Task<List<EventListDto>> GetMyEventsAsync(string userId)
		{
			return Task.FromResult(new List<EventListDto>());
		}
	}
}