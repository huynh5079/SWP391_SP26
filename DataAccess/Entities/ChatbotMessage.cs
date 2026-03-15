using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Enum;

namespace DataAccess.Entities
{
	public class ChatbotMessage : BaseEntity
	{
		//public string Id { get; set; } = Guid.NewGuid().ToString();
		public RoleEnum Role { get; set; }
		[Required]
		[MaxLength(450)]
		public string SessionId { get; set; } = null!;

		[Required]
		[MaxLength(20)]
		public string Sender { get; set; } = null!;

		[Required]
		public string Content { get; set; } = null!;

		[MaxLength(50)]
		public ChatMessageStatus Status { get; set; } = ChatMessageStatus.Final;

		[MaxLength(1000)]
		public string? ErrorMessage { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey(nameof(SessionId))]
		public virtual ChatbotSession Session { get; set; } = null!;
	}
}
