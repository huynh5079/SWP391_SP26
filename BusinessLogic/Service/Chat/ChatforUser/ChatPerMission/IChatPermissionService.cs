using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Chat.ChatforUser.ChatPerMission
{
    public interface IChatPermissionService
    {
        HashSet<string> GetAllowedRoles(string currentRole);
        bool CanChat(string senderRole, string receiverRole);
    }
}
