using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;
using Microsoft.AspNetCore.Http;

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

		// Team Management
		Task<bool> CreateEventTeamAsync(string eventId, string teamName, string? description);
		Task<bool> DeleteEventTeamAsync(string teamId);
		Task<bool> AddMemberToTeamAsync(string teamId, string? studentUserId, string? staffUserId, string roleName);
		Task<bool> RemoveMemberFromTeamAsync(string memberId);
		Task<List<EventTeamDto>> GetEventTeamsAsync(string eventId);

		// Paged deleted events (soft-deleted)
		Task<PagedResult<EventListDto>> GetMyDeletedEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10);
		
		// Agendas and Documents
		Task<string> CreateEventAgendaAsync(string userId, CreateEventAgendaDto dto);
		Task<string> CreateEventDocumentAsync(string userId, CreateEventDocumentDto dto);

        // Get expired events for organizer
        Task<List<EventListDto>> GetExpiredEventsAsync(string userId);

        Task<string?> UpdateThumbnailAsync(string eventId, IFormFile file, string userId);
        Task<string> AddEventImageAsync(string eventId, IFormFile file, string userId);
        Task RemoveEventImageAsync(string eventId, string imageUrl, string userId);
	}
}
