namespace BusinessLogic.DTOs.Ticket
{
    public class CheckInRequestDto
    {
        public string QrPayload { get; set; } = null!;
        public string EventId { get; set; } = null!;
    }
}
