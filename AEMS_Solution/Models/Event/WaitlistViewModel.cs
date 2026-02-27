using System;

namespace AEMS_Solution.Models.Event
{
    // Dedicated Waitlist ViewModel used across views
    public class WaitlistViewModel
    {
        public string Id { get; set; } = "";
        public string EventId { get; set; } = "";

        public string StudentId { get; set; } = "";
        public StudentMiniVm? Student { get; set; }

        public DateTime JoinedAt { get; set; }
        public bool IsNotified { get; set; }
    }
}
