using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role.Organizer;

public class EventParticipantDto
{
    public string TicketId { get; set; } = null!;
    public string? TicketCode { get; set; }
    public string StudentId { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public TicketStatusEnum Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
}
