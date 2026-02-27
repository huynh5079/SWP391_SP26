using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Event;

public interface IEventWaitlistService
{
    Task AddToWaitlistAsync(AddToWaitlistRequestDto dto);
    Task RemoveFromWaitlistAsync(string studentId, string eventId);
    Task<List<EventWaitlistDto>> GetWaitlistByEventAsync(string eventId);
    Task OfferNextAsync(string eventId);
    Task RespondToOfferAsync(RespondOfferRequestDto dto);
}
