using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AEMS_Solution.Models.Event.EventAgenda
{
	public class MyAgendaViewModel
	{
		public string PageTitle { get; set; } = "My Agenda";
		public string PageDescription { get; set; } = "Quản lý agenda của các sự kiện do bạn tổ chức.";
		public bool IsReadOnly { get; set; }
		public string? Search { get; set; }
		public string? SelectedEventId { get; set; }
		public List<SelectListItem> EventOptions { get; set; } = new();
		public List<AgendaItemViewModel> Agendas { get; set; } = new();
	}

	public class AgendaItemViewModel
	{
		public string Id { get; set; } = string.Empty;
		public string EventId { get; set; } = string.Empty;
		public string EventTitle { get; set; } = string.Empty;
		public string OrganizerName { get; set; } = string.Empty;
		public string? SessionName { get; set; }
		public string? Description { get; set; }
		public string? SpeakerInfo { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string? Location { get; set; }
		public EventTypeEnum? EventType { get; set; }
		public EventStatusEnum EventStatus { get; set; }
	}

	public class EditAgendaViewModel
	{
		public string Id { get; set; } = string.Empty;
		public string EventId { get; set; } = string.Empty;
		public string EventTitle { get; set; } = string.Empty;

		[Required(ErrorMessage = "Vui lòng nhập Session Name")]
		public string SessionName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Vui lòng nhập Speaker")]
		public string SpeakerInfo { get; set; } = string.Empty;

		public string? Description { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string? Location { get; set; }
	}
}
