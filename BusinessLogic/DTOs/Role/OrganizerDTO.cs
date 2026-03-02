using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.Role.Organizer;

public class OrganizerDto
{
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int DraftEvents { get; set; }
    
    // New stats requested by view
    public int RegistrationsToday { get; set; }
    public decimal DepositCollectedThisMonth { get; set; }
    public List<string> RegistrationTrendLabels { get; set; } = new();
    public List<int> RegistrationTrendData { get; set; } = new();
    public Dictionary<string, int> EventStatusDistribution { get; set; } = new();

    public List<EventItemDto> UpcomingList { get; set; } = new();
    
    public List<EventFeedbackSummaryDto> RecentFeedbacks { get; set; } = new();
}

public class EventFeedbackSummaryDto
{
    public string EventId { get; set; } = "";
    public string EventTitle { get; set; } = "";
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? StudentId { get; set; }
    public string? StudentCode { get; set; }
}

// Simple paged result used by services
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// DTOs used to return dropdown data for Create/Update event views
public class SelectItemDto
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
}

public class CreateEventDropdownsDto
{
    public List<SelectItemDto> Semesters { get; set; } = new();
    public List<SelectItemDto> Departments { get; set; } = new();
    public List<SelectItemDto> Locations { get; set; } = new();
    public List<SelectItemDto> Topics { get; set; } = new();
}