using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AEMS_Solution.Models.Event
{
	public class CreateEventViewModel
	{
		// ===== 1. Basic Info =====
		[Required, StringLength(200)]
		public string Title { get; set; } = "";

		public string? Description { get; set; }

		public string? ThumbnailUrl { get; set; } //*

		// ===== 2. Time =====
		[Required]
		public DateTime StartTime { get; set; } = DateTime.Now.AddDays(1);

		[Required]
		public DateTime EndTime { get; set; } = DateTime.Now.AddDays(1).AddHours(2);

		// ===== 3. Relations =====
		[Required(ErrorMessage = "Vui lòng chọn Semester")]
		public string SemesterId { get; set; } = "";

		public string? DepartmentId { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn Location")]
		public string LocationId { get; set; } = "";

		public string? TopicId { get; set; }

		// ===== 4. Capacity & Deposit =====
		[Range(1, 200000)]
		public int MaxCapacity { get; set; } = 100;

		public bool IsDepositRequired { get; set; } = false;

		[Range(0, 999999999)]
		public decimal DepositAmount { get; set; } = 0;

		// ===== 5. Type / Status / Mode =====
		public EventTypeEnum Type { get; set; } = EventTypeEnum.Workshop;

		public EventStatusEnum Status { get; set; } = EventStatusEnum.Draft;

		public EventModeEnum Mode { get; set; } = EventModeEnum.Hybrid;

		public string? MeetingUrl { get; set; }

		// ===== 6. Child Collections =====
		public List<CreateAgendaItemVm> Agendas { get; set; } = new();

		public List<CreateDocumentVm> Documents { get; set; } = new();

		// ===== 7. Dropdown sources (UI only) =====
		public List<SelectListItem> Semesters { get; set; } = new();
		public List<SelectListItem> Departments { get; set; } = new();
		public List<SelectListItem> Locations { get; set; } = new();
		public List<SelectListItem> Topics { get; set; } = new();
	}

	public class CreateAgendaItemVm
	{
		public string? SessionName { get; set; }
		public string? Description { get; set; }
		public string? SpeakerName { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string? Location { get; set; } // agenda location text (bảng agenda của bạn là nvarchar)
	}

	public class CreateDocumentVm
	{
		public string? FileName { get; set; }
		public string? Url { get; set; }
		public string? Type { get; set; }
	}
}