using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.ValidationDataforEvent;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Event;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;
    private readonly IEventValidator _validator;

    public EventService(IUnitOfWork uow, IEventValidator validator)
    {
        _uow = uow;
        _validator = validator;
    }

    public async Task<List<EventListDto>> GetMyEventsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("UserId không được để trống.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null)
            throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên để tạo hồ sơ của bạn.");

        var events = await _uow.Events.GetAllAsync(
            e => e.OrganizerId == staff.Id,
            q => q
                .Include(x => x.Tickets)
                .Include(x => x.EventWaitlists)
                .Include(x => x.Feedbacks)
                .Include(x => x.Semester)
                .Include(x => x.Department)
                .Include(x => x.Location)
        );

        var list = new List<EventListDto>();
        foreach (var e in events.OrderByDescending(x => x.StartTime))
        {
            int ticketCount = e.Tickets?.Count ?? 0;
            int checkedIn = e.Tickets?.Count(t => t.CheckInTime != null) ?? 0;
            int waitlist = e.EventWaitlists?.Count ?? 0;
            double avg = 0;
            if (e.Feedbacks != null && e.Feedbacks.Count > 0)
            {
                var ratings = e.Feedbacks.Where(f => f.Rating != null).Select(f => f.Rating!.Value).ToList();
                if (ratings.Count > 0) avg = ratings.Average();
            }

            list.Add(new EventListDto
            {
                EventId = e.Id,
                Title = e.Title,
                ThumbnailUrl = e.ThumbnailUrl,
                SemesterId = e.SemesterId,
                SemesterName = e.Semester?.Name,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name,
                Location = e.Location?.Name ?? e.LocationId,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                MaxCapacity = e.MaxCapacity,
                Status = e.Status?.ToString() ?? "",
                RegisteredCount = ticketCount,
                CheckedInCount = checkedIn,
                WaitlistCount = waitlist,
                AvgRating = avg,
                Mode = e.Mode?.ToString(),
                MeetingUrl = e.MeetingUrl,
            });
        }

        return list;
    }

    public async Task<PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, string? status, string? semesterId, int page = 1, int pageSize = 10)
    {
        var items = await GetMyEventsAsync(userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items.Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            items = items.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(semesterId))
        {
            items = items.Where(x => x.SemesterId == semesterId).ToList();
        }

        var total = items.Count;
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<EventListDto>
        {
            Items = paged,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task CreateEventAsync(string userId, CreateEventRequestDto dto)
    {
        _validator.ValidateCreate(dto);
        // Validate deposit rules
        _validator.ValidateDeposit(dto);
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        if (!string.IsNullOrEmpty(dto.TopicId))
        {
            var topic = await _uow.Topics.GetByIdAsync(dto.TopicId!);
            if (topic == null) throw new InvalidOperationException("Topic không tồn tại.");
        }

        var location = await _uow.Locations.GetByIdAsync(dto.LocationId);
        if (location == null) throw new InvalidOperationException("Location không tồn tại.");

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            var now = DateTimeHelper.GetVietnamTime();
            var entity = new DataAccess.Entities.Event
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title?.Trim() ?? "",
                Description = dto.Description,
                ThumbnailUrl = dto.BannerUrl,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TopicId = dto.TopicId,
                LocationId = dto.LocationId,
                OrganizerId = staff.Id,
                SemesterId = dto.SemesterId,
                DepartmentId = dto.DepartmentId,
                MaxCapacity = dto.Capacity,
                IsDepositRequired = dto.IsDepositRequired,
                DepositAmount = dto.DepositAmount,
                Type = dto.Type,
                Mode = dto.Mode,
                MeetingUrl = dto.MeetingUrl,
                Status = dto.Status ?? EventStatusEnum.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _uow.Events.CreateAsync(entity);

            // Validate agendas via validator (skips empty agendas)
            _validator.ValidateAgendas(dto.Agendas);
            if (dto.Agendas != null && dto.Agendas.Count > 0)
            {
                foreach (var a in dto.Agendas)
                {
                    bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerName)
                        && string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
                    if (isEmpty) continue;

                    await _uow.EventAgenda.CreateAsync(new EventAgenda
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = entity.Id,
                        SessionName = a.SessionName,
                        Description = a.Description,
                        SpeakerName = a.SpeakerName,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        Location = a.Location,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateEventAsync(string userId, string eventId, UpdateEventRequestDto dto)
    {
        _validator.ValidateUpdate(dto);
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền sửa event này.");

        var topic = await _uow.Topics.GetByIdAsync(dto.TopicId);
        if (topic == null) throw new InvalidOperationException("Topic không tồn tại.");

        var location = await _uow.Locations.GetByIdAsync(dto.LocationId);
        if (location == null) throw new InvalidOperationException("Location không tồn tại.");

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.StartTime = dto.StartTime;
            ev.EndTime = dto.EndTime;
            ev.TopicId = dto.TopicId;
            ev.LocationId = dto.LocationId;
            ev.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _uow.Events.UpdateAsync(ev);
            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteEventAsync(string userId, string eventId)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền xóa event này.");

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Events.RemoveAsync(ev);
            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SendForApprovalAsync(string userId, string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
            throw new InvalidOperationException("Event id không hợp lệ.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null)
            throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null)
            throw new InvalidOperationException("Event không tồn tại.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền thực hiện hành động này.");

        if (ev.Status == EventStatusEnum.Pending)
            throw new InvalidOperationException("Event đã ở trạng thái Pending.");

        if (ev.Status != EventStatusEnum.Draft)
            throw new InvalidOperationException("Chỉ event ở trạng thái Draft mới có thể gửi duyệt.");

        ev.Status = EventStatusEnum.Pending;
        ev.UpdatedAt = DateTimeHelper.GetVietnamTime();

        using var tx = await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Events.UpdateAsync(ev);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<EventDetailsDto> GetEventDetailsAsync(string eventId, string? userId = null)
    {
        if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("Event id không hợp lệ.");

        var ev = (await _uow.Events.GetAllAsync(e => e.Id == eventId,
            q => q.Include(x => x.EventAgenda)
                  .Include(x => x.Tickets)
                  .Include(x => x.EventWaitlists)
                  .Include(x => x.Feedbacks)
                  .Include(x => x.Semester)
                  .Include(x => x.Department)
                  .Include(x => x.Location))).FirstOrDefault();

        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        var dto = new EventDetailsDto
        {
            EventId = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            ThumbnailUrl = ev.ThumbnailUrl,
            SemesterName = ev.Semester?.Name,
            DepartmentName = ev.Department?.Name,
            Location = ev.Location?.Name ?? ev.LocationId,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            MaxCapacity = ev.MaxCapacity,
            IsDepositRequired = ev.IsDepositRequired ?? false,
            DepositAmount = ev.DepositAmount ?? 0,
            RegisteredCount = ev.Tickets?.Count ?? 0,
            CheckedInCount = ev.Tickets?.Count(t => t.CheckInTime != null) ?? 0,
            WaitlistCount = ev.EventWaitlists?.Count ?? 0,
            AvgRating = (ev.Feedbacks != null && ev.Feedbacks.Count > 0) ? ev.Feedbacks.Where(f => f.Rating != null).Select(f => f.Rating!.Value).DefaultIfEmpty(0).Average() : 0
        };

        if (ev.EventAgenda != null)
        {
            foreach (var a in ev.EventAgenda.OrderBy(x => x.StartTime))
            {
                dto.Agendas.Add(new EventAgendaDto
                {
                    Id = a.Id,
                    EventId = a.EventId,
                    SessionName = a.SessionName ?? "",
                    Description = a.Description,
                    SpeakerName = a.SpeakerName,
                    StartTime = a.StartTime ?? DateTime.MinValue,
                    EndTime = a.EndTime ?? DateTime.MinValue,
                    Location = a.Location
                });
            }
        }

        if (!string.IsNullOrEmpty(userId))
        {
            var staff = await _uow.StaffProfiles.GetAsync(s => s.UserId == userId);
            if (staff != null && ev.OrganizerId == staff.Id)
            {
                dto.CanEdit = true;
                dto.CanSendForApproval = ev.Status == EventStatusEnum.Draft;
            }
        }

        return dto;
    }
}

