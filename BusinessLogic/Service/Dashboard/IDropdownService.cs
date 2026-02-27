using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.Dashboard
{
    public interface IDropdownService
    {
        Task<CreateEventDropdownsDto> GetCreateEventDropdownsAsync();
    }
}