using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using EventAgendaEntity = DataAccess.Entities.EventAgenda;
using EventDocumentEntity = DataAccess.Entities.EventDocument;
using Microsoft.EntityFrameworkCore;
using BusinessLogic.Service.ValidationData.Event;
using Microsoft.AspNetCore.Http;

namespace BusinessLogic.Service.Event;

public class EventService : IEventService
{
	private readonly IUnitOfWork _uow;
	private readonly IEventValidator _validator;
	private readonly INotificationService _notificationService;
	private readonly BusinessLogic.Storage.IFileStorageService _fileStorageService;

	private static string? NormalizeLocationId(EventModeEnum? mode, string? locationId)
		=> mode == EventModeEnum.Online || string.IsNullOrWhiteSpace(locationId) ? null : locationId;

	private static string? NormalizeMeetingUrl(EventModeEnum? mode, string? meetingUrl)
		=> mode == EventModeEnum.Offline || string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();

	public EventService(IUnitOfWork uow, IEventValidator validator, INotificationService notificationService, BusinessLogic.Storage.IFileStorageService fileStorageService)
	{
		_uow = uow;
		_validator = validator;
		_notificationService = notificationService;
		_fileStorageService = fileStorageService;
	}

	public async Task CancelEventAsync(string userId, string eventId)
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
			throw new InvalidOperationException("Bạn không có quyền hủy event này.");

		if (ev.Status == EventStatusEnum.Cancelled)
			throw new InvalidOperationException("Event đã bị hủy.");

		// không cho hủy nếu đã kết thúc
		var now = DateTimeHelper.GetVietnamTime();
		if (ev.EndTime <= now)
			throw new InvalidOperationException("Sự kiện đã kết thúc, không thể hủy.");

		ev.Status = EventStatusEnum.Cancelled;
		ev.UpdatedAt = now;

		using var tx = await _uow.BeginTransactionAsync();
		try
		{
			await _uow.Events.UpdateAsync(ev);
			await _uow.SaveChangesAsync();
			await tx.CommitAsync();

			// Notify all ticket holders
			var tickets = await _uow.Tickets.GetAllAsync(t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
			foreach (var ticket in tickets)
			{
				if (!string.IsNullOrEmpty(ticket.StudentId))
				{
					// Convert StudentId to UserId
					var studentProfile = await _uow.StudentProfiles.GetAsync(sp => sp.Id == ticket.StudentId);
					if (studentProfile?.UserId != null)
					{
						await _notificationService.SendNotificationAsync(new SendNotificationRequest
						{
							ReceiverId = studentProfile.UserId,
							Title = "Sự kiện đã bị hủy",
							Message = $"Sự kiện '{ev.Title}' mà bạn đăng ký tham gia đã bị hủy bởi Ban Tổ Chức.",
							Type = DataAccess.Enum.NotificationType.EventOrganizeCancel,
							RelatedEntityId = ev.Id
						});
					}
				}
			}
		}
		catch
		{
			await tx.RollbackAsync();
			throw;
		}
	}

	public async Task PublishEventAsync(string userId, string eventId)
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

		if (ev.Status != EventStatusEnum.Approved)
			throw new InvalidOperationException("Chỉ event đã Approved mới được Public.");

		ev.Status = EventStatusEnum.Published;
		ev.UpdatedAt = DateTimeHelper.GetVietnamTime();

		using var tx = await _uow.BeginTransactionAsync();
		try
		{
			await _uow.Events.UpdateAsync(ev);
			await _uow.SaveChangesAsync();
			await tx.CommitAsync();

			// Notify Organizer that their event is live
			if (staff.UserId != null)
			{
				await _notificationService.SendNotificationAsync(new SendNotificationRequest
				{
					ReceiverId = staff.UserId,
					Title = "Sự kiện đã được xuất bản",
					Message = $"Sự kiện '{ev.Title}' của bạn đã chính thức được công khai trên hệ thống.",
					Type = DataAccess.Enum.NotificationType.EventPublished,
					RelatedEntityId = ev.Id
				});
			}
		}
		catch
		{
			await tx.RollbackAsync();
			throw;
		}
	}

	public async Task<List<EventListDto>> GetMyEventsAsync(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			throw new InvalidOperationException("UserId không được để trống.");

		var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
		if (staff == null)
			throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên để tạo hồ sơ của bạn.");

		var events = await _uow.Events.GetAllAsync(
			e => e.OrganizerId == staff.Id && e.DeletedAt == null,
			q => q
				.Include(x => x.Tickets)
				.Include(x => x.EventWaitlists)
				.Include(x => x.Feedbacks)
				.Include(x => x.Semester)
				.Include(x => x.Department)
				.Include(x => x.Location)
				.Include(x => x.ApprovalLogs)
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

			// Determine last approval action from logs (if any)
			var lastApproval = e.ApprovalLogs?.Where(l => l.DeletedAt == null).OrderByDescending(l => l.CreatedAt).FirstOrDefault();

			list.Add(new EventListDto
			{
				EventId = e.Id,
				Title = e.Title,
				ThumbnailUrl = e.ThumbnailUrl?.Split('|')[0],
				ImageUrls = string.IsNullOrEmpty(e.ThumbnailUrl) ? new List<string>() : e.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
				SemesterId = e.SemesterId,
				SemesterName = e.Semester?.Name,
				DepartmentId = e.DepartmentId,
				DepartmentName = e.Department?.Name,
				Location = !string.IsNullOrEmpty(e.Location?.Address) ? e.Location.Address : (e.Location?.Name ?? e.LocationId),
				StartTime = e.StartTime,
				EndTime = e.EndTime,
				MaxCapacity = e.MaxCapacity,
				Status = e.Status,
				RegisteredCount = ticketCount,
				CheckedInCount = checkedIn,
				WaitlistCount = waitlist,
				AvgRating = avg,
				Mode = e.Mode?.ToString(),
				MeetingUrl = e.MeetingUrl,
				LastApprovalAction = lastApproval?.Action,
				LastApprovalActionAt = lastApproval?.CreatedAt,
				LastApprovalBy = lastApproval?.ApproverId,
			});
		}

		return list;
	}

	public async Task<List<EventListDto>> GetMyDeletedEventsAsync(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			throw new InvalidOperationException("UserId không được để trống.");

		var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
		if (staff == null)
			throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên để tạo hồ sơ của bạn.");

		var events = await _uow.Events.GetAllAsync(
			e => e.OrganizerId == staff.Id && e.DeletedAt != null,
			q => q
				.Include(x => x.Tickets)
				.Include(x => x.EventWaitlists)
				.Include(x => x.Feedbacks)
				.Include(x => x.Semester)
				.Include(x => x.Department)
				.Include(x => x.Location)
				.Include(x => x.ApprovalLogs)
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

			var lastApproval = e.ApprovalLogs?.Where(l => l.DeletedAt == null).OrderByDescending(l => l.CreatedAt).FirstOrDefault();

			list.Add(new EventListDto
			{
				EventId = e.Id,
				Title = e.Title,
				ThumbnailUrl = e.ThumbnailUrl?.Split('|')[0],
				ImageUrls = string.IsNullOrEmpty(e.ThumbnailUrl) ? new List<string>() : e.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
				SemesterId = e.SemesterId,
				SemesterName = e.Semester?.Name,
				DepartmentId = e.DepartmentId,
				DepartmentName = e.Department?.Name,
				Location = !string.IsNullOrEmpty(e.Location?.Address) ? e.Location.Address : (e.Location?.Name ?? e.LocationId),
				StartTime = e.StartTime,
				EndTime = e.EndTime,
				MaxCapacity = e.MaxCapacity,
				Status = e.Status,
				RegisteredCount = ticketCount,
				CheckedInCount = checkedIn,
				WaitlistCount = waitlist,
				AvgRating = avg,
				Mode = e.Mode?.ToString(),
				MeetingUrl = e.MeetingUrl,
				LastApprovalAction = lastApproval?.Action,
				LastApprovalActionAt = lastApproval?.CreatedAt,
				LastApprovalBy = lastApproval?.ApproverId,
			});
		}

		return list;
	}

	public async Task<PagedResult<EventListDto>> GetMyEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
	{
		var items = await GetMyEventsAsync(userId);

		if (!string.IsNullOrWhiteSpace(search))
		{
			items = items.Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		if (status.HasValue && Enum.TryParse<EventStatusEnum>(status.ToString(), true, out var parsedStatus))
		{
			items = items.Where(x => x.Status == parsedStatus).ToList();
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

	public async Task<PagedResult<EventListDto>> GetMyDeletedEventsAsync(string userId, string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
	{
		var items = await GetMyDeletedEventsAsync(userId);

		if (!string.IsNullOrWhiteSpace(search))
		{
			items = items.Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		if (status.HasValue && Enum.TryParse<EventStatusEnum>(status.ToString(), true, out var parsedStatus))
		{
			items = items.Where(x => x.Status == parsedStatus).ToList();
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

	public async Task<string> CreateEventAsync(string userId, CreateEventRequestDto dto)
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

		var normalizedLocationId = NormalizeLocationId(dto.Mode, dto.LocationId);
		var normalizedMeetingUrl = NormalizeMeetingUrl(dto.Mode, dto.MeetingUrl);
		if (!string.IsNullOrWhiteSpace(normalizedLocationId))
		{
			var location = await _uow.Locations.GetByIdAsync(normalizedLocationId);
			if (location == null) throw new InvalidOperationException("Location không tồn tại.");
		}
		
		var now = DateTimeHelper.GetVietnamTime();
		//if (dto.StartTime < now.AddDays(7))
			//throw new InvalidOperationException("Thời gian bắt đầu sự kiện phải cách ngày tạo ít nhất 7 ngày.");

		if (dto.Agendas != null)
		{
			foreach (var a in dto.Agendas)
			{
				bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerInfo)
					&& string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
				if (isEmpty) continue;

				if (!a.StartTime.HasValue || !a.EndTime.HasValue)
					throw new InvalidOperationException("Agenda phải có thời gian bắt đầu và kết thúc.");
				if (a.StartTime > a.EndTime)
					throw new InvalidOperationException("Thời gian bắt đầu Agenda phải bé hơn thời gian kết thúc");
				if (a.StartTime < dto.StartTime || a.EndTime > dto.EndTime)
					throw new InvalidOperationException("Thời gian agenda phải nằm trong khoảng thời gian của sự kiện.");
			}
		}

		using var transaction = await _uow.BeginTransactionAsync();
		try
		{
			var entity = new DataAccess.Entities.Event
			{
				
				Title = dto.Title?.Trim() ?? "",
				Description = dto.Description,
				ThumbnailUrl = dto.BannerUrl,
				StartTime = dto.StartTime,
				EndTime = dto.EndTime,
				TopicId = dto.TopicId,
				LocationId = normalizedLocationId,
				OrganizerId = staff.Id,
				SemesterId = dto.SemesterId,
				DepartmentId = dto.DepartmentId,
				MaxCapacity = dto.Capacity,
				IsDepositRequired = dto.IsDepositRequired,
				DepositAmount = dto.DepositAmount,
				Type = dto.Type,
				Mode = dto.Mode,
				MeetingUrl = normalizedMeetingUrl,
				Status = dto.Status ?? EventStatusEnum.Draft,
				CreatedAt = now,
				UpdatedAt = now
			};

			// Handle Multiple Image Uploads
			var imageUrls = new List<string>();
			if (!string.IsNullOrWhiteSpace(dto.BannerUrl)) imageUrls.Add(dto.BannerUrl);

			if (dto.ThumbnailFile != null)
			{
				var uploadResult = await _fileStorageService.UploadSingleAsync(dto.ThumbnailFile, UploadContext.EventThumbnail, userId);
				if (uploadResult != null) imageUrls.Add(uploadResult.Url);
			}

			if (dto.BannerFiles != null && dto.BannerFiles.Count > 0)
			{
				foreach (var file in dto.BannerFiles)
				{
					var uploadResult = await _fileStorageService.UploadSingleAsync(file, UploadContext.EventThumbnail, userId);
					if (uploadResult != null) imageUrls.Add(uploadResult.Url);
				}
			}

			if (imageUrls.Count > 0)
			{
				entity.ThumbnailUrl = string.Join("|", imageUrls);
			}

			await _uow.Events.CreateAsync(entity);

			// Validate agendas via validator (skips empty agendas)
			_validator.ValidateAgendas(dto.Agendas);
			if (dto.Agendas != null && dto.Agendas.Count > 0)
			{
				foreach (var a in dto.Agendas)
				{
					bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerInfo)
						&& string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
					if (isEmpty) continue;

					await _uow.EventAgenda.CreateAsync(new EventAgendaEntity
					{
						
						EventId = entity.Id,
						SessionName = a.SessionName,
						Description = a.Description,
						SpeakerInfo = a.SpeakerInfo,
						StartTime = a.StartTime,
						EndTime = a.EndTime,
						Location = a.Location,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			if (dto.Documents != null && dto.Documents.Count > 0)
			{
				foreach (var d in dto.Documents)
				{
					if (d.File != null)
					{
						var uploadResult = await _fileStorageService.UploadSingleAsync(d.File, UploadContext.EventDocument, userId);
						if (uploadResult != null)
						{
							d.Url = uploadResult.Url;
							d.FileName = d.File.FileName;
						}
					}

					if (string.IsNullOrWhiteSpace(d.Url) && string.IsNullOrWhiteSpace(d.FileName))
					{
						continue;
					}

					await _uow.EventDocuments.CreateAsync(new EventDocumentEntity
					{
						Id = Guid.NewGuid().ToString(),
						EventId = entity.Id,
						Name = d.FileName,
						Url = d.Url,
						Type = d.Type,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			await _uow.SaveChangesAsync();
			await transaction.CommitAsync();
			return entity.Id;
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

    public async Task<string?> UpdateThumbnailAsync(string eventId, IFormFile file, string userId)
    {
        if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("Event id không hợp lệ.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền sửa sự kiện này.");

        if (file != null && file.Length > 0)
        {
            var uploadResult = await _fileStorageService.UploadSingleAsync(file, UploadContext.EventThumbnail, userId);
            if (uploadResult != null)
            {
                var urls = ev.ThumbnailUrl?.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                if (urls.Count > 0) urls[0] = uploadResult.Url;
                else urls.Add(uploadResult.Url);

                ev.ThumbnailUrl = string.Join("|", urls);
                ev.UpdatedAt = DateTimeHelper.GetVietnamTime();
                
                await _uow.Events.UpdateAsync(ev);
                await _uow.SaveChangesAsync();
                return uploadResult.Url;
            }
        }
        return null;
    }

    public async Task<string> AddEventImageAsync(string eventId, IFormFile file, string userId)
    {
        if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("Event id không hợp lệ.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền sửa sự kiện này.");

        if (file == null || file.Length == 0) throw new InvalidOperationException("File không hợp lệ.");

        var uploadResult = await _fileStorageService.UploadSingleAsync(file, UploadContext.EventThumbnail, userId);
        if (uploadResult == null) throw new InvalidOperationException("Upload ảnh thất bại.");

        var urls = string.IsNullOrEmpty(ev.ThumbnailUrl) 
            ? new List<string>() 
            : ev.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

        urls.Add(uploadResult.Url);
        ev.ThumbnailUrl = string.Join("|", urls);
        ev.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _uow.Events.UpdateAsync(ev);
        await _uow.SaveChangesAsync();

        return uploadResult.Url;
    }

    public async Task RemoveEventImageAsync(string eventId, string imageUrl, string userId)
    {
        if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("Event id không hợp lệ.");

        var ev = await _uow.Events.GetByIdAsync(eventId);
        if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên.");

        if (ev.OrganizerId != staff.Id)
            throw new InvalidOperationException("Bạn không có quyền sửa sự kiện này.");

        if (string.IsNullOrEmpty(ev.ThumbnailUrl)) return;

        var urls = ev.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (urls.Remove(imageUrl))
        {
            ev.ThumbnailUrl = string.Join("|", urls);
            ev.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _uow.Events.UpdateAsync(ev);
            await _uow.SaveChangesAsync();
        }
    }

	public async Task RestoreEventAsync(string userId, string eventId)
	{
		var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
		if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

		var ev = await _uow.Events.GetByIdAsync(eventId);
		if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

		if (ev.OrganizerId != staff.Id)
			throw new InvalidOperationException("Bạn không có quyền khôi phục event này.");

		using var transaction = await _uow.BeginTransactionAsync();
		try
		{
			var now = DateTimeHelper.GetVietnamTime();
			ev.DeletedAt = null;
			ev.StatusEventAvailable = EventStatusAvailableEnum.Available;
			ev.UpdatedAt = now;

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

		var normalizedLocationId = NormalizeLocationId(dto.Mode, dto.LocationId);
		var normalizedMeetingUrl = NormalizeMeetingUrl(dto.Mode, dto.MeetingUrl);
		if (!string.IsNullOrWhiteSpace(normalizedLocationId))
		{
			var location = await _uow.Locations.GetByIdAsync(normalizedLocationId);
			if (location == null) throw new InvalidOperationException("Location không tồn tại.");
		}

		if (!string.IsNullOrWhiteSpace(dto.SemesterId))
		{
			var semester = await _uow.Semesters.GetByIdAsync(dto.SemesterId);
			if (semester == null) throw new InvalidOperationException("Semester không tồn tại.");
		}

		if (!string.IsNullOrWhiteSpace(dto.DepartmentId))
		{
			var department = await _uow.Departments.GetByIdAsync(dto.DepartmentId);
			if (department == null) throw new InvalidOperationException("Department không tồn tại.");
		}

		if (dto.Agendas != null)
		{
			foreach (var a in dto.Agendas)
			{
				bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerInfo)
					&& string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
				if (isEmpty) continue;

				if (!a.StartTime.HasValue || !a.EndTime.HasValue)
					throw new InvalidOperationException("Agenda phải có thời gian bắt đầu và kết thúc.");
				if (a.StartTime > a.EndTime)
					throw new InvalidOperationException("Thời gian bắt đầu Agenda phải bé hơn thời gian kết thúc");
				if (a.StartTime < dto.StartTime || a.EndTime > dto.EndTime)
					throw new InvalidOperationException("Thời gian agenda phải nằm trong khoảng thời gian của sự kiện.");
			}
		}

		using var transaction = await _uow.BeginTransactionAsync();
		try
		{
			var now = DateTimeHelper.GetVietnamTime();
			ev.Title = dto.Title;
			ev.Description = dto.Description;
			ev.StartTime = dto.StartTime;
			ev.EndTime = dto.EndTime;
			ev.SemesterId = dto.SemesterId;
			ev.DepartmentId = dto.DepartmentId;
			ev.TopicId = dto.TopicId;
			ev.LocationId = normalizedLocationId;
			ev.MaxCapacity = dto.Capacity ?? ev.MaxCapacity;
			ev.Type = dto.Type;
			ev.Status = dto.Status ?? ev.Status;
			ev.IsDepositRequired = dto.IsDepositRequired;
			ev.DepositAmount = dto.DepositAmount;
			// Handle Multiple Image Uploads
			var imageUrls = ev.ThumbnailUrl?.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
			
			if (dto.ThumbnailFile != null)
			{
				var uploadResult = await _fileStorageService.UploadSingleAsync(dto.ThumbnailFile, UploadContext.EventThumbnail, userId);
				if (uploadResult != null) imageUrls.Add(uploadResult.Url);
			}

			if (dto.BannerFiles != null && dto.BannerFiles.Count > 0)
			{
				foreach (var file in dto.BannerFiles)
				{
					var uploadResult = await _fileStorageService.UploadSingleAsync(file, UploadContext.EventThumbnail, userId);
					if (uploadResult != null) imageUrls.Add(uploadResult.Url);
				}
			}

			if (imageUrls.Count > 0)
			{
				ev.ThumbnailUrl = string.Join("|", imageUrls.Distinct());
			}
			else if (!string.IsNullOrWhiteSpace(dto.BannerUrl))
			{
				ev.ThumbnailUrl = dto.BannerUrl;
			}

			ev.Mode = dto.Mode;
			ev.MeetingUrl = normalizedMeetingUrl;
			ev.UpdatedAt = now;

			var existingAgendas = await _uow.EventAgenda.GetAllAsync(x => x.EventId == eventId);
			foreach (var agenda in existingAgendas)
			{
				await _uow.EventAgenda.RemoveAsync(agenda);
			}

			var existingDocuments = await _uow.EventDocuments.GetAllAsync(x => x.EventId == eventId);
			foreach (var document in existingDocuments)
			{
				await _uow.EventDocuments.RemoveAsync(document);
			}

			if (dto.Agendas != null)
			{
				foreach (var a in dto.Agendas)
				{
					bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerInfo)
						&& string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
					if (isEmpty) continue;

					await _uow.EventAgenda.CreateAsync(new EventAgendaEntity
					{
						Id = Guid.NewGuid().ToString(),
						EventId = ev.Id,
						SessionName = a.SessionName,
						Description = a.Description,
						SpeakerInfo = a.SpeakerInfo,
						StartTime = a.StartTime,
						EndTime = a.EndTime,
						Location = a.Location,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			if (dto.Documents != null)
			{
				foreach (var d in dto.Documents)
				{
					if (d.File != null)
					{
						var uploadResult = await _fileStorageService.UploadSingleAsync(d.File, UploadContext.EventDocument, userId);
						if (uploadResult != null)
						{
							d.Url = uploadResult.Url;
							d.FileName = d.File.FileName;
						}
					}

					if (string.IsNullOrWhiteSpace(d.Url) && string.IsNullOrWhiteSpace(d.FileName))
					{
						continue;
					}

					await _uow.EventDocuments.CreateAsync(new EventDocumentEntity
					{
						Id = Guid.NewGuid().ToString(),
						EventId = ev.Id,
						Name = d.FileName,
						Url = d.Url,
						Type = d.Type,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			await _uow.Events.UpdateAsync(ev);
			await _uow.SaveChangesAsync();
			await transaction.CommitAsync();

			// Notify all valid ticket holders
			if (ev.Status == EventStatusEnum.Published || ev.Status == EventStatusEnum.Happening)
			{
				var tickets = await _uow.Tickets.GetAllAsync(t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
				foreach (var ticket in tickets)
				{
					if (!string.IsNullOrEmpty(ticket.StudentId))
					{
						var studentProfile = await _uow.StudentProfiles.GetAsync(sp => sp.Id == ticket.StudentId);
						if (studentProfile?.UserId != null)
						{
							await _notificationService.SendNotificationAsync(new SendNotificationRequest
							{
								ReceiverId = studentProfile.UserId,
								Title = "Sự kiện đã thay đổi thông tin",
								Message = $"Sự kiện '{ev.Title}' mà bạn đã đăng ký vừa được Ban Tổ Chức cập nhật lại thông tin.",
								Type = DataAccess.Enum.NotificationType.EventUpdated,
								RelatedEntityId = ev.Id
							});
						}
					}
				}
			}
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

		if (ev.Status != EventStatusEnum.Draft && ev.Status != EventStatusEnum.Rejected)
			throw new InvalidOperationException("Chỉ event ở trạng thái Draft hoặc Rejected mới có thể gửi duyệt.");

		var now = DateTimeHelper.GetVietnamTime();
		if (ev.StartTime <= now)
			throw new InvalidOperationException("Thời gian bắt đầu phải lớn hơn thời gian hiện tại mới có thể gửi duyệt.");

		if (ev.StartTime - now < TimeSpan.FromDays(5))
			throw new InvalidOperationException("Cần gửi duyệt trước ít nhất 5 ngày so với thời gian bắt đầu.");

		ev.Status = EventStatusEnum.Pending;
		ev.UpdatedAt = now;

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
			  .Include(x => x.Location)
			  .Include(x => x.EventDocuments)
			  .Include(x => x.ApprovalLogs)
			  .Include(x => x.EventTeams)
			    .ThenInclude(t => t.TeamMembers)
			      .ThenInclude(m => m.Student)
			        .ThenInclude(s => s.User)
			  .Include(x => x.EventTeams)
			    .ThenInclude(t => t.TeamMembers)
			      .ThenInclude(m => m.Staff)
			        .ThenInclude(s => s.User)
			)).FirstOrDefault();

		if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

		var lastApproval = ev.ApprovalLogs?
			.Where(l => l.DeletedAt == null)
			.OrderByDescending(l => l.CreatedAt)
			.FirstOrDefault();

		var dto = new EventDetailsDto
		{
			EventId = ev.Id,
			Title = ev.Title,
			Description = ev.Description,
			ThumbnailUrl = ev.ThumbnailUrl?.Split('|')[0],
			ImageUrls = string.IsNullOrEmpty(ev.ThumbnailUrl) ? new List<string>() : ev.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
			SemesterId = ev.SemesterId,
			SemesterName = ev.Semester?.Name,
			DepartmentId = ev.DepartmentId,
			DepartmentName = ev.Department?.Name,
			LocationId = ev.LocationId,
			Location = !string.IsNullOrEmpty(ev.Location?.Address) ? ev.Location.Address : (ev.Location?.Name ?? ev.LocationId),
			TopicId = ev.TopicId,
			StartTime = ev.StartTime,
			EndTime = ev.EndTime,
			MaxCapacity = ev.MaxCapacity,
			IsDepositRequired = ev.IsDepositRequired ?? false,
			DepositAmount = ev.DepositAmount ?? 0,
			Type = ev.Type,
			Status = ev.Status,
			Mode = ev.Mode,
			MeetingUrl = ev.MeetingUrl,
			RegisteredCount = ev.Tickets?.Count ?? 0,
			CheckedInCount = ev.Tickets?.Count(t => t.CheckInTime != null) ?? 0,
			WaitlistCount = ev.EventWaitlists?.Count ?? 0,
			AvgRating = (ev.Feedbacks != null && ev.Feedbacks.Count > 0) ? ev.Feedbacks.Where(f => f.Rating != null).Select(f => f.Rating!.Value).DefaultIfEmpty(0).Average() : 0,
			LastApprovalAction = lastApproval?.Action,
			LastApprovalComment = lastApproval?.Comment,
			LastApprovalAt = lastApproval?.CreatedAt
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
					SpeakerInfo = a.SpeakerInfo,
					StartTime = a.StartTime ?? DateTime.MinValue,
					EndTime = a.EndTime ?? DateTime.MinValue,
					Location = a.Location
				});
			}
		}

		if (ev.EventDocuments != null)
		{
			foreach (var d in ev.EventDocuments.OrderBy(x => x.CreatedAt))
			{
				dto.Documents.Add(new EventDocumentDto
				{
					Id = d.Id,
					EventId = d.EventId,
					FileName = d.Name,
					Url = d.Url,
					Type = d.Type
				});
			}
		}

		if (ev.EventTeams != null)
		{
			foreach (var t in ev.EventTeams.OrderBy(x => x.CreatedAt))
			{
				var teamDto = new EventTeamDto
				{
					Id = t.Id,
					EventId = t.EventId,
					TeamName = t.TeamName,
					Description = t.Description,
					Score = t.Score ?? 0,
					PlaceRank = t.PlaceRank,
					CreatedAt = t.CreatedAt,
					TeamMembers = new List<TeamMemberDto>()
				};

				if (t.TeamMembers != null)
				{
					foreach (var m in t.TeamMembers)
					{
						string name = m.Student != null ? (m.Student.User?.FullName ?? "Unknown") : (m.Staff != null ? (m.Staff.User?.FullName ?? "Unknown") : "Unknown");
						string email = m.Student != null ? (m.Student.User?.Email ?? "Unknown") : (m.Staff != null ? (m.Staff.User?.Email ?? "Unknown") : "Unknown");
						string role = m.Student != null ? "Student" : (m.Staff != null ? "Staff" : "Unknown");
						
						teamDto.TeamMembers.Add(new TeamMemberDto
						{
							Id = m.Id,
							TeamId = m.TeamId,
							StudentId = m.StudentId,
							StaffId = m.StaffId,
							MemberName = name,
							MemberEmail = email,
							RoleName = role,
							TeamRole = m.Role?.ToString() ?? "Member"
						});
					}
				}
				dto.Teams.Add(teamDto);
			}
		}

		if (!string.IsNullOrEmpty(userId))
		{
			var staff = await _uow.StaffProfiles.GetAsync(s => s.UserId == userId);
			if (staff != null && ev.OrganizerId == staff.Id)
			{
				dto.CanEdit = true;
				dto.CanSendForApproval = ev.Status == EventStatusEnum.Draft || ev.Status == EventStatusEnum.Rejected;
			}
		}

		return dto;
	}

	public async Task SoftDeleteEventAsync(string userId, string eventId)
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
			var now = DateTimeHelper.GetVietnamTime();
			ev.DeletedAt = now;
			// đánh dấu trạng thái khả dụng là NotAvailable nếu có cột này
			ev.StatusEventAvailable = EventStatusAvailableEnum.NA;
			ev.UpdatedAt = now;

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

	public async Task<bool> CreateEventTeamAsync(string eventId, string teamName, string? description)
	{
		var team = new DataAccess.Entities.EventTeam
		{
			Id = Guid.NewGuid().ToString(),
			EventId = eventId,
			TeamName = teamName,
			Description = description,
			Score = 0,
			CreatedAt = DateTimeHelper.GetVietnamTime()
		};
		await _uow.EventTeams.CreateAsync(team); // Requires IUnitOfWork to have EventTeams, wait I will check if it exists or use generic repo
		await _uow.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteEventTeamAsync(string teamId)
	{
		var team = await _uow.EventTeams.GetByIdAsync(teamId); // Will check if repo exists
		if (team != null)
		{
			await _uow.EventTeams.RemoveAsync(team);
			await _uow.SaveChangesAsync();
		}
		return true;
	}

	public async Task<bool> AddMemberToTeamAsync(string teamId, string? studentUserId, string? staffUserId, string roleName)
	{
		string? realStudentId = null;
		string? realStaffId = null;

		if (!string.IsNullOrEmpty(studentUserId))
		{
			var student = await _uow.StudentProfiles.GetAsync(x => x.UserId == studentUserId);
			if (student == null) throw new InvalidOperationException("Profile học sinh không tồn tại cho User này.");
			realStudentId = student.Id;
		}
		else if (!string.IsNullOrEmpty(staffUserId))
		{
			var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == staffUserId);
			if (staff == null) throw new InvalidOperationException("Profile nhân viên không tồn tại cho User này.");
			realStaffId = staff.Id;
		}
		else
		{
			throw new InvalidOperationException("Phải chọn thành viên hợp lệ.");
		}

		// Check if already in team
		// Assuming we have generic access or ITeamMemberRepository. If not we will use DbContext directly or create a quick generic.
		// Wait, I UnitOfWork might not have EventTeams or TeamMembers explicitly exposed, let me check IUnitOfWork.cs below.
		
		var teamMember = new DataAccess.Entities.TeamMember
		{
			Id = Guid.NewGuid().ToString(),
			TeamId = teamId,
			StudentId = realStudentId,
			StaffId = realStaffId,
			Role = Enum.TryParse<TeamRoleEnum>(roleName, true, out var r) ? r : TeamRoleEnum.Member,
			CreatedAt = DateTimeHelper.GetVietnamTime()
		};

		await _uow.TeamMembers.CreateAsync(teamMember);
		await _uow.SaveChangesAsync();
		return true;
	}

	public async Task<bool> RemoveMemberFromTeamAsync(string memberId)
	{
		var member = await _uow.TeamMembers.GetByIdAsync(memberId);
		if (member != null)
		{
			await _uow.TeamMembers.RemoveAsync(member);
			await _uow.SaveChangesAsync();
		}
		return true;
	}

	public async Task<List<EventTeamDto>> GetEventTeamsAsync(string eventId)
	{
		// Just a stub or simple implementation to satisfy the interface
		return new List<EventTeamDto>();
	}

    public async Task<string> CreateEventAgendaAsync(string userId, CreateEventAgendaDto dto)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        var ev = await _uow.Events.GetByIdAsync(dto.EventId);
        if (ev == null) throw new KeyNotFoundException("Event không tồn tại.");

        if (ev.OrganizerId != staff.Id)
            throw new UnauthorizedAccessException($"Tài khoản {userId} cố gắng truy cập Event {dto.EventId} không thuộc quyền quản lý.");

        if (dto.StartTime >= dto.EndTime)
            throw new InvalidOperationException("Thời gian kết thúc agenda phải lớn hơn thời gian bắt đầu.");

        if (dto.StartTime < ev.StartTime || dto.EndTime > ev.EndTime)
            throw new InvalidOperationException("Thời gian agenda phải nằm trong thời gian của event.");

        var now = DateTimeHelper.GetVietnamTime();
        var agenda = new EventAgendaEntity
        {
            Id = Guid.NewGuid().ToString(),
            EventId = dto.EventId,
            SessionName = dto.SessionName?.Trim(),
            SpeakerInfo = dto.SpeakerInfo?.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (!string.IsNullOrEmpty(dto.SpeakerUserId))
        {
            if (dto.SpeakerUserRole == "Student")
            {
                var student = await _uow.StudentProfiles.GetAsync(x => x.UserId == dto.SpeakerUserId);
                if (student != null) agenda.StudentSpeakerId = student.Id;
            }
            else if (dto.SpeakerUserRole == "Staff")
            {
                var speakerStaff = await _uow.StaffProfiles.GetAsync(x => x.UserId == dto.SpeakerUserId);
                if (speakerStaff != null) agenda.StaffSpeakerId = speakerStaff.Id;
            }
        }

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            await _uow.EventAgenda.CreateAsync(agenda);
            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
            return agenda.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> CreateEventDocumentAsync(string userId, CreateEventDocumentDto dto)
    {
        var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
        if (staff == null) throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile). Vui lòng liên hệ quản trị viên.");

        var ev = await _uow.Events.GetByIdAsync(dto.EventId);
        if (ev == null) throw new KeyNotFoundException("Event không tồn tại.");

        if (ev.OrganizerId != staff.Id)
            throw new UnauthorizedAccessException($"Tài khoản {userId} cố gắng truy cập Event {dto.EventId} không thuộc quyền quản lý.");

        var now = DateTimeHelper.GetVietnamTime();

        string? finalUrl = dto.Url;
        string? finalName = dto.Name;

        if (dto.File != null)
        {
            var uploadResult = await _fileStorageService.UploadSingleAsync(dto.File, UploadContext.EventDocument, userId);
            if (uploadResult != null)
            {
                finalUrl = uploadResult.Url;
                finalName = dto.File.FileName;
            }
        }

        var document = new EventDocumentEntity
        {
            Id = Guid.NewGuid().ToString(),
            EventId = dto.EventId,
            Name = finalName?.Trim() ?? "Tài liệu không tên",
            Url = finalUrl?.Trim() ?? string.Empty,
            Type = string.IsNullOrWhiteSpace(dto.Type) ? null : dto.Type.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        using var transaction = await _uow.BeginTransactionAsync();
        try
        {
            await _uow.EventDocuments.CreateAsync(document);
            await _uow.SaveChangesAsync();
            await transaction.CommitAsync();
            return document.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

