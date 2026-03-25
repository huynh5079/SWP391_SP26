namespace BusinessLogic.Service.Chat.ChatforUser
{
    public interface IChatPresenceTracker
    {
        void UserConnected(string userId);
        void UserDisconnected(string userId);
        bool IsOnline(string userId);
    }
}
