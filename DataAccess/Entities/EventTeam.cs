using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventTeam : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string TeamName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Score { get; set; }

    public int? PlaceRank { get; set; }

    //public DateTime? CreatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
