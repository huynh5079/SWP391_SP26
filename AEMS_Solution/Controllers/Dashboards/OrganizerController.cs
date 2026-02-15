using System.Security.Claims;
using AEMS_Solution.Models.Organizer;
using DataAccess.Entities;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin,Staff,Organizer")]
public class OrganizerController : Controller
{
	private readonly AEMSContext _context;

	public OrganizerController(AEMSContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index()
	{
		// Lấy UserId từ claim chuẩn
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		// Tìm StaffProfile.Id của user hiện tại
		var staffId = await _context.StaffProfiles
			.Where(s => s.UserId == userId)
			.Select(s => s.Id)
			.FirstOrDefaultAsync();

		var now = DateTime.Now;

		var myEventsQuery = _context.Events.Where(e => e.OrganizerId == staffId);

		var total = await myEventsQuery.CountAsync();
		var upcoming = await myEventsQuery.CountAsync(e => e.StartTime >= now);

		// Status của bạn đang lưu string enum (HasConversion<string>())
		var draft = await myEventsQuery.CountAsync(e => e.Status == EventStatusEnum.Draft);

		var recent = await myEventsQuery
			.OrderByDescending(e => e.CreatedAt)
			.Take(6)
			.Select(e => new OrganizerEventRow
			{
				Id = e.Id,
				Title = e.Title,
				StartTime = e.StartTime,
				Status = e.Status != null ? e.Status.ToString() : "Draft"
			})
			.ToListAsync();

		var vm = new OrganizerDashboardViewModel
		{
			TotalEvents = total,
			UpcomingEvents = upcoming,
			DraftEvents = draft,
			RecentEvents = recent
		};

		return View(vm);
	}
}
