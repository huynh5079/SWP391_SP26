using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role.Organizer;

public class CreateEventRequestDto
{
	// ===== 1. Basic Info =====
	[Required, MaxLength(200)]
	public string Title { get; set; } = "";

	[MaxLength(4000)]
	public string Description { get; set; } = "";

	public string? BannerUrl { get; set; } //*

	// ===== 2. Time =====
	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public DateTime RegistrationOpenTime { get; set; }

	public DateTime RegistrationCloseTime { get; set; }

	// ===== 3. Relations =====
	public string SemesterId { get; set; } = "";

	public string? DepartmentId { get; set; }

	public string TopicId { get; set; } = "";

	public string LocationId { get; set; } = "";

	// ===== 4. Capacity & Deposit =====
	public int Capacity { get; set; }

	public bool IsDepositRequired { get; set; }

	public decimal DepositAmount { get; set; }

	// ===== 5. Type / Status / Mode =====
	public EventTypeEnum? Type { get; set; }

	public EventStatusEnum? Status { get; set; }

	public EventModeEnum? Mode { get; set; }

	public string? MeetingUrl { get; set; }

	// ===== 6. Child Collections =====
	public List<CreateAgendaItemDto> Agendas { get; set; } = new();
	public List<CreateDocumentDto> Documents { get; set; } = new();
}

public class CreateAgendaItemDto
{
    public string? SessionName { get; set; }
    public string? Description { get; set; }
    public string? SpeakerInfo { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
}

public class CreateDocumentDto
{
	public string? FileName { get; set; }
	public string? Url { get; set; }
	public string? Type { get; set; }
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
    public string? LastApprovalComment { get; set; }
	public List<UpdateAgendaItemDto> Agendas { get; set; } = new();
	public List<UpdateDocumentDto> Documents { get; set; } = new();
}

public class UpdateAgendaItemDto
{
	public string? Id { get; set; }
	public string? SessionName { get; set; }
	public string? Description { get; set; }
	public string? SpeakerInfo { get; set; }
	public DateTime? StartTime { get; set; }
	public DateTime? EndTime { get; set; }
	public string? Location { get; set; }
}

public class UpdateDocumentDto
{
	public string? Id { get; set; }
	public string? FileName { get; set; }
	public string? Url { get; set; }
	public string? Type { get; set; }
}

// Soft-delete DTO: toggle availability instead of physical delete
public class SoftDeleteEventRequestDto
{
    [Required]
    public string EventId { get; set; } = "";

    // NotAvailable = soft delete; Available = restore
    [Required]
    public EventStatusAvailableEnum StatusEventAvailable { get; set; }
}

