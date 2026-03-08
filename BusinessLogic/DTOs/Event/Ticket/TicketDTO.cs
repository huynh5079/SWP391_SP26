using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Ticket
{
	public class TicketDTO
	{
		public string Id { get; set; } = "";
		public string EventId { get; set; } = "";
		public string StudentId { get; set; } = "";

		public string? EventName { get; set; }
		public string? TicketCode { get; set; }
		public TicketStatusEnum Status { get; set; }

		public DateTime? CheckInTime { get; set; }
		
		public string StudentName
		{
			get => StudentName;
			set => StudentName = value;
		}

	}

	public class CreateTicketDTO
	{
		public string EventId { get; set; } = "";
		public string StudentId { get; set; } = "";
		public string? TicketCode { get; set; }
		public TicketStatusEnum Status { get; set; } = TicketStatusEnum.Registered;
	}

	public class UpdateTicketDTO
	{
		public string? TicketCode { get; set; }
		public TicketStatusEnum? Status { get; set; }
		public DateTime? CheckInTime { get; set; }
	}
}
