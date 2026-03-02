using BusinessLogic.DTOs.Ticket;

namespace BusinessLogic.Service.Organizer
{
    public interface ICheckInService
    {
        Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId);
    }
}
