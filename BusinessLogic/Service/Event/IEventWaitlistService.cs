using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Event;

public interface IEventWaitlistService
{
    Task AddToWaitlistAsync(AddToWaitlistRequestDto dto);
    Task RemoveFromWaitlistAsync(string studentId, string eventId);
    Task<List<EventWaitlistDto>> GetWaitlistByEventAsync(string eventId);
    Task OfferNextAsync(string eventId);
    Task NotifyOfferedStudentAsync(string eventId, string studentId);
    Task RespondToOfferAsync(RespondOfferRequestDto dto);
    Task<List<EventWaitlistDto>> GetMyWaitlistAsync(string userId);
}
