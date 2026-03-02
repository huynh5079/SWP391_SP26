namespace BusinessLogic.DTOs.Ticket
{
    public class CheckInResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
    }
}
