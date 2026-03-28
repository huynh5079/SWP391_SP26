using System;
<<<<<<< HEAD
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    [Table("UserActivityLog")]
    public class UserActivityLog : BaseEntity
    {
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? TargetId { get; set; }

        [MaxLength(100)]
        public string? TargetType { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
=======
using DataAccess.Enum;

namespace DataAccess.Entities
{
    public partial class UserActivityLog : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public UserActionType ActionType { get; set; }
        public string? TargetId { get; set; }
        public TargetType TargetType { get; set; }
        public string? Description { get; set; }
        
        public virtual User User { get; set; } = null!;
>>>>>>> origin/123-feat-fix
    }
}
