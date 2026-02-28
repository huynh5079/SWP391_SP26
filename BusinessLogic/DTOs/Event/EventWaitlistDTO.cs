using System;
using DataAccess.Entities;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role.Organizer;

public class EventWaitlistDto
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string EventTitle { get; set; } = "";
    public DateTime? EventStartTime { get; set; }

    public string StudentId { get; set; } = "";
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public string? StudentEmail { get; set; }

    public DateTime? JoinedAt { get; set; }
    public bool? IsNotified { get; set; }

    public EventWaitlistStatusEnum Status { get; set; } 
    public DateTime? OfferedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public int? Position { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EventWaitlistListItemDto
{
    public string Id { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string? StudentName { get; set; }
    public int? Position { get; set; }
    public EventWaitlistStatusEnum Status { get; set; }
    public DateTime? JoinedAt { get; set; }
}

public class AddToWaitlistRequestDto
{
    public string EventId { get; set; } = "";
    public string StudentId { get; set; } = ""; // optional if caller infers from user
}

public class RemoveFromWaitlistRequestDto
{
    public string EventId { get; set; } = "";
    public string StudentId { get; set; } = "";
}

public class RespondOfferRequestDto
{
    public string EventId { get; set; } = "";
    public string StudentId { get; set; } = "";
    public bool Accept { get; set; }
}

