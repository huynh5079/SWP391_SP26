using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class TeamMember: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string TeamId { get; set; } = null!;

    public string? StudentId { get; set; }

    public string? StaffId { get; set; }

    public TeamRoleEnum? Role { get; set; }

    //public DateTime? JoinedAt { get; set; }

    public virtual StudentProfile? Student { get; set; }

    public virtual StaffProfile? Staff { get; set; }

    public virtual EventTeam Team { get; set; } = null!;
}
