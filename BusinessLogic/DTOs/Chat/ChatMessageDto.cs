namespace BusinessLogic.DTOs.Chat
{
    public class ChatMessageDto
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRecalled { get; set; }
        public bool IsReadByReceiver { get; set; }
    }
}
