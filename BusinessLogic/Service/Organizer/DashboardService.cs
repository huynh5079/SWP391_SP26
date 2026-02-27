using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Interfaces;
using DataAccess.Repositories.Abstraction;
using DataAccess.Enum;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Organizer;

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

        var total = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id);
        var upcoming = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);
        var draft = await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.Status == EventStatusEnum.Draft);

        var upcomingList = await _uow.Events.GetAllAsync(e => e.OrganizerId == staff.Id && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);

        var top5 = upcomingList.OrderBy(e => e.StartTime).Take(5).Select(e => new EventItemDto
        {
            Id = e.Id,
            Title = e.Title,
            StartTime = e.StartTime,
            Status = e.Status.ToString()
        }).ToList();

        return new OrganizerDto
        {
            TotalEvents = total,
            UpcomingEvents = upcoming,
            DraftEvents = draft,
            UpcomingList = top5
        };
    }

    public async Task<int> GetTotalEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

        return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id);
    }

    public async Task<int> GetUpcomingEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

        var now = DateTimeHelper.GetVietnamTime();
        return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.Status == EventStatusEnum.Upcoming && e.StartTime >= now);
    }

    public async Task<int> GetDraftEventAsync(string userId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) return 0;

        return await _uow.Events.CountAsync(e => e.OrganizerId == staff.Id && e.Status == EventStatusEnum.Draft);
    }
}