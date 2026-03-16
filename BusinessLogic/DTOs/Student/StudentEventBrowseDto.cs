using DataAccess.Enum;

namespace BusinessLogic.DTOs.Student
{
    /// <summary>
    /// Lightweight DTO used in the weekly calendar browse view.
    /// </summary>
    public class StudentEventBrowseDto
    {
        public string EventId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public EventStatusEnum Status { get; set; }
        public int MaxCapacity { get; set; }
        public int RegisteredCount { get; set; }
        public int AvailableSeats => Math.Max(0, MaxCapacity - RegisteredCount);
        public bool IsFull => AvailableSeats == 0;
        public string? TopicName { get; set; }
        public string? SemesterName { get; set; }
        public EventModeEnum? Mode { get; set; }

        // Per-student registration status (null = not registered)
        public bool IsRegistered { get; set; }
        
        // Custom Role (e.g. Khách tham gia, Ban tổ chức, Diễn giả)
        public string? ParticipationRole { get; set; }
    }
}
