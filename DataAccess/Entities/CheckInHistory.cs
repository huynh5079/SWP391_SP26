using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class CheckInHistory : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string TicketId { get; set; } = null!;

    public string? ScannerId { get; set; }

    public string? DeviceName { get; set; }

    public ScanTypeEnum? ScanType { get; set; }

    public string? Location { get; set; }

    public virtual StaffProfile? Scanner { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;
}
