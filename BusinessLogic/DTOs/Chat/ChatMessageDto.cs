namespace BusinessLogic.DTOs.Chat
{
    public class ChatMessageDto
    {
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
