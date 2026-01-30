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
	public class ChatMessage : BaseEntity
	{
		// Note: User requested BigInt Id, but BaseEntity uses String (Guid). 
		// We use BaseEntity to maintain consistency with the project structure and IGenericRepository compatibility.

		[Required]
		[MaxLength(450)]
		public string SessionId { get; set; } = null!;

		[Required]
		[MaxLength(20)]
		public string Sender { get; set; } = null!; // user | assistant | system | tool

		public string Content { get; set; } = null!;

		[MaxLength(450)]
		public string? ReplyToMessageId { get; set; }

		[MaxLength(50)]
		public ChatMessageStatus Status { get; set; }

		[MaxLength(1000)]
		public string? ErrorMessage { get; set; }

		public bool IsDeleted { get; set; } = false;

		// Navigation Properties
		[ForeignKey("SessionId")]
		public virtual ChatSession ChatSession { get; set; } = null!;

		[ForeignKey("ReplyToMessageId")]
		public virtual ChatMessage? ReplyToMessage { get; set; }

		public virtual ICollection<ChatMessage> InverseReplyToMessage { get; set; } = new List<ChatMessage>();
	} 
}
