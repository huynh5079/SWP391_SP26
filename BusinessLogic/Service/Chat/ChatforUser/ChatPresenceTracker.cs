using System.Collections.Concurrent;

namespace BusinessLogic.Service.Chat.ChatforUser
{
    public class ChatPresenceTracker : IChatPresenceTracker
    {
        private readonly ConcurrentDictionary<string, int> _connections = new();

        public void UserConnected(string userId)
        {
            _connections.AddOrUpdate(userId, 1, (_, current) => current + 1);
        }

        public void UserDisconnected(string userId)
        {
            _connections.AddOrUpdate(userId, 0, (_, current) => Math.Max(0, current - 1));

            if (_connections.TryGetValue(userId, out var count) && count == 0)
            {
                _connections.TryRemove(userId, out _);
            }
        }

        public bool IsOnline(string userId)
        {
            return _connections.TryGetValue(userId, out var count) && count > 0;
        }
    }
}
