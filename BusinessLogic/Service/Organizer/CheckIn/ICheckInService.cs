using BusinessLogic.DTOs.Ticket;

namespace BusinessLogic.Service.Organizer.CheckIn
{
    public interface ICheckInService
    {
        Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId);
        Task<CheckInResponseDto> ProcessCheckoutAsync(CheckInRequestDto request, string organizerUserId);
    }
}
