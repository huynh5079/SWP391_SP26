using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventAgenda : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? SessionName { get; set; }

    public string? Description { get; set; }

    public string? SpeakerInfo { get; set; }

    public string? StudentSpeakerId { get; set; }
    public virtual StudentProfile? StudentSpeaker { get; set; }

    public string? StaffSpeakerId { get; set; }
    public virtual StaffProfile? StaffSpeaker { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Location { get; set; }

    public virtual Event Event { get; set; } = null!;
}
