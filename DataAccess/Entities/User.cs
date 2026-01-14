using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class User: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string RoleId { get; set; } = null!;

    public UserStatusEnum? Status { get; set; }

    public bool? IsBanned { get; set; }

    public string? GoogleId { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    //public DateTime? DeletedAt { get; set; }

    public virtual StudentProfile Id1 { get; set; } = null!;

    public virtual StaffProfile IdNavigation { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role Role { get; set; } = null!;
}
