using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Role : BaseEntity
{
    //public string Id { get; set; } = null!;

    public RoleEnum? RoleName { get; set; } = null!;

    //public DateTime? CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
