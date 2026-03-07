using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Chat;

namespace BusinessLogic.Service.Chat.ChatforUser
{
    public interface IChatUserService
    {
        Task<IReadOnlyList<ChatContactDto>> GetContactsAsync(string currentUserId, string currentRole);
        Task<IReadOnlyList<ChatMessageDto>> GetConversationAsync(string currentUserId, string currentRole, string otherUserId);
        Task<ChatMessageDto> SendPrivateMessageAsync(string senderUserId, string senderRole, string receiverUserId, string content);
        Task MarkConversationReadAsync(string userId, string otherUserId);
    }
}
