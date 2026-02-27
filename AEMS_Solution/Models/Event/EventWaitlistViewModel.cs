using System;
using BusinessLogic.DTOs.Role.Organizer;

namespace AEMS_Solution.Models.Event
{
    // Dedicated Waitlist ViewModel used across views
    public class EventWaitlistViewModel
    {
		public string EventId { get; set; } = "";
		public List<BusinessLogic.DTOs.Role.Organizer.EventWaitlistDto> Items { get; set; }
			= new();
	}
}
