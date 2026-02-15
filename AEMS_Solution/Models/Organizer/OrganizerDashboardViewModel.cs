using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Entities; // sửa theo namespace entity của bạn
using AEMS_Solution.Models.Organizer;
namespace AEMS_Solution.Models.Organizer
{
    public class OrganizerDashboardViewModel
	{
		public int TotalEvents { get; set; }
		public int UpcomingEvents { get; set; }
		public int RegistrationsToday { get; set; }
		public decimal DepositCollectedThisMonth { get; set; }
		public int DraftEvents { get; set; }

		public List<string> RegistrationTrendLabels { get; set; } = new();
		public List<int> RegistrationTrendData { get; set; } = new();

		public Dictionary<string, int> EventStatusDistribution { get; set; } = new();
		public List<OrganizerEventRow> RecentEvents { get; set; } = new();
	}
	public class OrganizerEventRow
	{
		public string Id { get; set; } = "";
		public string Title { get; set; } = "";
		public DateTime StartTime { get; set; }
		public string Status { get; set; } = "";
	}

}
