using DataAccess.Entities;
using DataAccess.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
	public class ChatSession : BaseEntity
	{
		// Id is inherited from BaseEntity (string/Guid)

		[Required]
		[MaxLength(450)]
		public string UserId { get; set; } = null!;

		[MaxLength(255)]
		public string? Title { get; set; }

		[MaxLength(50)]
		public ChatSessionStatus Status { get; set; }

		public bool IsDeleted { get; set; } = false;

		// Navigation Properties
		[ForeignKey("UserId")]
		public virtual User User { get; set; } = null!;

		public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
	}
}