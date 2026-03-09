using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Organizer.Manage
{
    public class TicketListItemVm
    {
        public string TicketId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string? EventName { get; set; }
        public string? TicketCode { get; set; }
        public TicketStatusEnum Status { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string StudentName { get; set; } = string.Empty;
    }

    public class TicketIndexViewModel
    {
        public string? EventId { get; set; }
        public string? StudentId { get; set; }
        public List<TicketListItemVm> Tickets { get; set; } = new();
    }

    public class CreateTicketViewModel
    {
        [Required]
        public string EventId { get; set; } = string.Empty;

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public string? TicketCode { get; set; }
        public TicketStatusEnum Status { get; set; } = TicketStatusEnum.Registered;
    }

    public class UpdateTicketViewModel
    {
        [Required]
        public string TicketId { get; set; } = string.Empty;
        public string? EventName { get; set; }
        public string? StudentName { get; set; }
        public string? TicketCode { get; set; }
        public TicketStatusEnum? Status { get; set; }
        public DateTime? CheckInTime { get; set; }
    }
}
