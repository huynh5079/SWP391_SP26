using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Semester
{
	public class SemesterDTO
	{
		public string? Name { get; set; }

		public string? Code { get; set; }

		public DateTime? StartDate { get; set; }

		public DateTime? EndDate { get; set; }

		public SemesterStatusEnum Status { get; set; }

		public List<EventListDto> EventList { get; set; } = new();
	}
}
