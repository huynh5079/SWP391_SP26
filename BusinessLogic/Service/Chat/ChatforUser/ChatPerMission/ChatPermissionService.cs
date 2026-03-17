using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Chat.ChatforUser.ChatPerMission
{
	public class ChatPermissionService : IChatPermissionService
	{
		public HashSet<string> GetAllowedRoles(string currentRole)
		{
			return currentRole switch
			{
				"Admin" => new HashSet<string> { "Admin", "Staff", "Organizer", "Approver", "Approval", "Student" },
				"Staff" => new HashSet<string> { "Admin", "Organizer", "Approver", "Approval", "Student" },
				"Organizer" => new HashSet<string> { "Admin", "Staff", "Approver", "Approval", "Student" },
				"Approver" or "Approval" => new HashSet<string> { "Admin", "Staff", "Organizer", "Student" },
				"Student" => new HashSet<string> { "Admin", "Staff", "Organizer", "Student" },
				_ => new HashSet<string>()
			};
		}

		public bool CanChat(string senderRole, string receiverRole)
		{
			return GetAllowedRoles(senderRole).Contains(receiverRole);
		}
	}
}
