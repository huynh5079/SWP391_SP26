using DataAccess.Enum;

namespace BusinessLogic.DTOs
{
    public class SendNotificationRequest
    {
        public string ReceiverId { get; set; } // UserId format in DB is string
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public string? RelatedEntityId { get; set; }
    }
}
