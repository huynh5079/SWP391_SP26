using System;
using System.Collections.Generic;
using BusinessLogic.DTOs.Role.Organizer;

namespace AEMS_Solution.Models.Organizer.Manage
{
    public class OrganizerExpiredEventViewModel
    {
        public List<EventListDto> Events { get; set; } = new();
    }
}
