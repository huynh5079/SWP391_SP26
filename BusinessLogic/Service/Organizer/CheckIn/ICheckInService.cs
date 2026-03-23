using BusinessLogic.DTOs.Ticket;
using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Organizer.CheckIn
{
    public interface ICheckInService
    {
        Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId);
        Task<CheckInResponseDto> ProcessCheckoutAsync(CheckInRequestDto request, string organizerUserId);
        Task<List<EventParticipantDto>> GetParticipantsAsync(string eventId);
        Task ManualRegisterAsync(string eventId, string userId, string organizerUserId);
        Task CancelTicketAsync(string ticketId, string organizerUserId);
        Task ResendTicketEmailAsync(string ticketId, string organizerUserId);
    }
}
