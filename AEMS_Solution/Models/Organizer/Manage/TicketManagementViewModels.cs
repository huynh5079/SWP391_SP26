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

    public class TicketSalesByEventVm
    {
        public string EventId { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxCapacity { get; set; }
        public int SoldTickets { get; set; }
        public int RemainingSeats => Math.Max(0, MaxCapacity - SoldTickets);
        public double FillPercent => MaxCapacity > 0 ? Math.Round((double)SoldTickets / MaxCapacity * 100, 1) : 0;
    }

    public class ManageTicketViewModel
    {
        public string? Search { get; set; }
        public List<TicketSalesByEventVm> Events { get; set; } = new();
        public int TotalSoldTickets => Events.Sum(x => x.SoldTickets);
    }
}
