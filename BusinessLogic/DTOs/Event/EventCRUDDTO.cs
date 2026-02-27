using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role.Organizer;

public class CreateEventRequestDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";
    [MaxLength(4000)]
    public string Description { get; set; } = "";

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public DateTime RegistrationOpenTime { get; set; }
    public DateTime RegistrationCloseTime { get; set; }

    public string TopicId { get; set; } = "";
    public string LocationId { get; set; } = "";

    public int Capacity { get; set; }

    public EventModeEnum? Mode { get; set; }
    public string? MeetingUrl { get; set; }

    public string? BannerUrl { get; set; }

    // Match DB Event
    public string SemesterId { get; set; } = "";
    public string? DepartmentId { get; set; }

    public EventTypeEnum? Type { get; set; }

    public bool IsDepositRequired { get; set; }
    public decimal DepositAmount { get; set; }

    public EventStatusEnum? Status { get; set; }

    public List<CreateAgendaItemDto> Agendas { get; set; } = new();
}

public class CreateAgendaItemDto
{
    public string? SessionName { get; set; }
    public string? Description { get; set; }
    public string? SpeakerName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
}

public class UpdateEventRequestDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string TopicId { get; set; } = "";
    public string LocationId { get; set; } = "";

    public EventStatusEnum? Status { get; set; }

    public string? SemesterId { get; set; }
    public string? DepartmentId { get; set; }

    public int? Capacity { get; set; }

    public EventTypeEnum? Type { get; set; }
    public bool? IsDepositRequired { get; set; }
    public decimal? DepositAmount { get; set; }

    public string? BannerUrl { get; set; }
    public string? MeetingUrl { get; set; }
    public EventModeEnum? Mode { get; set; }

    public DateTime? RegistrationOpenTime { get; set; }
    public DateTime? RegistrationCloseTime { get; set; }
}

