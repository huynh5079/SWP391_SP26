using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class TeamMember: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string TeamId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    public TeamRoleEnum? Role { get; set; }

    //public DateTime? JoinedAt { get; set; }

    public virtual StudentProfile Student { get; set; } = null!;

    public virtual EventTeam Team { get; set; } = null!;
}
