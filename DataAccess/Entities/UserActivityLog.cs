using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAccess.Enum;

namespace DataAccess.Entities
{
    [Table("UserActivityLog")]
    public class UserActivityLog : BaseEntity
    {
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public UserActionType ActionType { get; set; }

        [MaxLength(450)]
        public string? TargetId { get; set; }

        public TargetType? TargetType { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
