using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Repositories.Abstraction;
using DataAccess.Enum;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<OrganizerDto> GetDashboardAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("UserId không được để trống.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null)
            throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên để tạo hồ sơ của bạn.");

        var now = DateTimeHelper.GetVietnamTime();

		var total = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null);
		var upcoming = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);
		var draft = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null && e.Status == EventStatusEnum.Draft);

		var upcomingList = await _uow.Events.GetAllAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);

        var top5 = upcomingList.OrderBy(e => e.StartTime).Take(5).Select(e => new EventItemDto
        {
            Id = e.Id,
            Title = e.Title,
            StartTime = e.StartTime,
            Status = e.Status
        }).ToList();

        // --- ENHANCEMENTS ---

        // 1. Registrations Today
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var recentTickets = await _uow.Tickets.GetAllAsync(
            t => t.Event != null && t.Event.OrganizerId == staff.Id &&
                 t.CreatedAt >= now.AddDays(-7) &&
                 t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled,
            q => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(q, x => x.Event!));
            
        var regsToday = recentTickets.Count(t => t.CreatedAt >= todayStart && t.CreatedAt < todayEnd);

        // 2. Deposit Collected This Month
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
		var allEvents = await _uow.Events.GetAllAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null);
        
        // Count deposits from active tickets for paid events this month
        var thisMonthTickets = await _uow.Tickets.GetAllAsync(
            t => t.Event != null && t.Event.OrganizerId == staff.Id &&
                 t.CreatedAt >= startOfMonth &&
                 t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled,
            q => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(q, x => x.Event!));
            
        var depositCollected = thisMonthTickets.Sum(t => t.Event!.DepositAmount.GetValueOrDefault());

        // 3. Registration Trend (Last 7 Days)
        var trendLabels = new List<string>();
        var trendData = new List<int>();
        for (int i = 6; i >= 0; i--)
        {
            var d = now.Date.AddDays(-i);
            trendLabels.Add(d.ToString("dd/MM"));
            trendData.Add(recentTickets.Count(t => t.CreatedAt >= d && t.CreatedAt < d.AddDays(1)));
        }

        // 4. Status Distribution
        var statusDist = allEvents
            .GroupBy(e => e.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // 5. Recent Feedbacks (Top 5)
        var recentFeedbacks = await _uow.Feedbacks.GetAllAsync(
            f => f.Event != null && f.Event.OrganizerId == staff.Id && f.DeletedAt == null,
            q => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(q, x => x.Event!)
                  .Include(x => x.Student!).ThenInclude(s => s.User!));

        var topFeedbacks = recentFeedbacks
            .OrderByDescending(f => f.CreatedAt)
            .Take(5)
            .Select(f => new BusinessLogic.DTOs.Role.Organizer.EventFeedbackSummaryDto
            {
                EventId = f.EventId,
                EventTitle = f.Event!.Title,
                Rating = (int)f.RatingEvent,
                Comment = f.Comment,
                CreatedAt = f.CreatedAt,
                StudentId = f.StudentId,
                StudentCode = f.Student?.StudentCode
            }).ToList();

        return new OrganizerDto
        {
            TotalEvents = total,
            UpcomingEvents = upcoming,
            DraftEvents = draft,
            UpcomingList = top5,
            
            RegistrationsToday = regsToday,
            DepositCollectedThisMonth = depositCollected,
            RegistrationTrendLabels = trendLabels,
            RegistrationTrendData = trendData,
            EventStatusDistribution = statusDist,
            RecentFeedbacks = topFeedbacks
        };
    }

    public async Task<int> GetTotalEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

		return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null);
    }

    public async Task<int> GetUpcomingEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

        var now = DateTimeHelper.GetVietnamTime();
		return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);
    }

    public async Task<int> GetDraftEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

		return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.DeletedAt == null && e.Status == EventStatusEnum.Draft);
    }
}