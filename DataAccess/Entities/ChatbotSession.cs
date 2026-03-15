using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Enum;

namespace DataAccess.Entities
{
	public class ChatbotSession :BaseEntity
	{
		//public string Id { get; set; } = Guid.NewGuid().ToString();
		public string UserId { get; set; } = null!;
		public DateTime StartedAt { get; set; } = DateTime.UtcNow;
		public DateTime? EndedAt { get; set; }
        public ChatSessionStatus Status { get; set; } // "Active", "Archived", etc.

		// Navigation property
		public virtual ICollection<ChatbotMessage> Messages { get; set; } = new List<ChatbotMessage>();
	}
}
