using BusinessLogic.DTOs.Role.Organizer;

namespace AEMS_Solution.Models.Organizer.Manage;

public class EventParticipantsViewModel
{
    public string EventId { get; set; } = "";
    public string EventTitle { get; set; } = "";
    public List<EventParticipantDto> Participants { get; set; } = new();
}
