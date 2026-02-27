using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Interfaces
{
    public interface IDropdownService
    {
        Task<CreateEventDropdownsDto> GetCreateEventDropdownsAsync();
    }
}