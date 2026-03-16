namespace BusinessLogic.DTOs.Student
{
    public class StudentEventFeedbackItemDto
    {
        public string EventId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
