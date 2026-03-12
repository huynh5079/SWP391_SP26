using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace BusinessLogic.Service.Event;

public class EventWaitlistService : IEventWaitlistService
{
	private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ISystemErrorLogService _errorLogService;

    public EventWaitlistService(IUnitOfWork uow, IEmailService emailService,
    INotificationService notificationService,
    ISystemErrorLogService errorLogService)
	{
		_uow = uow;
        _emailService = emailService;
        _notificationService = notificationService;
        _errorLogService = errorLogService;
    }

	public async Task AddToWaitlistAsync(AddToWaitlistRequestDto dto)
	{
		if (dto == null) throw new ArgumentNullException(nameof(dto));
		if (string.IsNullOrEmpty(dto.EventId) || string.IsNullOrEmpty(dto.StudentId))
			throw new InvalidOperationException("EventId và StudentId không được để trống.");

		var ev = await _uow.Events.GetByIdAsync(dto.EventId);
		if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

		var existing = await _uow.EventWaitlist.GetAsync(w => w.EventId == dto.EventId && w.StudentId == dto.StudentId);
		if (existing != null) throw new InvalidOperationException("Bạn đã nằm trong danh sách chờ của sự kiện này.");

		var count = await _uow.EventWaitlist.CountAsync(w => w.EventId == dto.EventId);
		var now = DateTimeHelper.GetVietnamTime();

		var entity = new EventWaitlist
		{
			Id = Guid.NewGuid().ToString(),
			EventId = dto.EventId,
			StudentId = dto.StudentId,
			JoinedAt = now,
			IsNotified = false,
			Status = DataAccess.Enum.EventWaitlistStatusEnum.Waiting,
			Position = count + 1,
			CreatedAt = now,
			UpdatedAt = now
		};

		await _uow.EventWaitlist.CreateAsync(entity);
		await _uow.SaveChangesAsync();
	}

	public async Task RemoveFromWaitlistAsync(string studentId, string eventId)
	{
		if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(eventId))
			throw new InvalidOperationException("EventId và StudentId không được để trống.");

		var entry = await _uow.EventWaitlist.GetAsync(w => w.EventId == eventId && w.StudentId == studentId);
		if (entry == null) return; // nothing to do

		await _uow.EventWaitlist.RemoveAsync(entry);
		await _uow.SaveChangesAsync();

		// Recompute positions for remaining entries
		var list = (await _uow.EventWaitlist.GetAllAsync(w => w.EventId == eventId, q => q.OrderBy(x => x.Position))).ToList();
		int pos = 1;
		foreach (var e in list)
		{
			e.Position = pos++;
			await _uow.EventWaitlist.UpdateAsync(e);
		}
		await _uow.SaveChangesAsync();
	}

	public async Task<List<EventWaitlistDto>> GetWaitlistByEventAsync(string eventId)
	{
		if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("EventId không được để trống.");

		var items = await _uow.EventWaitlist.GetAllAsync(w => w.EventId == eventId, q => q
			.Include(x => x.Student).ThenInclude(s => s.User)
			.Include(x => x.Event)
			.OrderBy(x => x.Position)
		);

		return items.Select(w => new EventWaitlistDto
		{
			Id = w.Id,
			EventId = w.EventId,
			EventTitle = w.Event?.Title ?? "",
			EventStartTime = w.Event?.StartTime,
			StudentId = w.StudentId,
			StudentName = w.Student?.User?.FullName,
			StudentCode = w.Student?.StudentCode,
			StudentEmail = w.Student?.User?.Email,
			JoinedAt = w.JoinedAt,
			IsNotified = w.IsNotified,
			OfferedAt = w.OfferedAt,
			RespondedAt = w.RespondedAt,
			Position = w.Position,
			Status = w.Status,
			CreatedAt = w.CreatedAt,
			UpdatedAt = w.UpdatedAt
		}).ToList();
	}

	public async Task OfferNextAsync(string eventId)
	{
		if (string.IsNullOrEmpty(eventId)) throw new InvalidOperationException("EventId không được để trống.");

        var next = (await _uow.EventWaitlist.GetAllAsync(
    w => w.EventId == eventId && w.Status == DataAccess.Enum.EventWaitlistStatusEnum.Waiting,
    q => q.Include(x => x.Student).ThenInclude(s => s.User)
           .OrderBy(x => x.Position))).FirstOrDefault();

        if (next == null) return;

		var now = DateTimeHelper.GetVietnamTime();
		next.Status = DataAccess.Enum.EventWaitlistStatusEnum.Offered;
		next.OfferedAt = now;
		next.IsNotified = true;
		next.UpdatedAt = now;

		await _uow.EventWaitlist.UpdateAsync(next);
		await _uow.SaveChangesAsync();

        // TODO: push notification/email to student
        await NotifyOfferedStudentAsync(eventId, next.StudentId);
    }
    public async Task NotifyOfferedStudentAsync(string eventId, string studentId)
    {
        try
        {
            // studentId ở đây là StudentProfile.Id → cần lấy User.Id
            var studentProfile = await _uow.StudentProfiles.GetAsync(
                s => s.Id == studentId,
                q => q.Include(x => x.User));

            var offeredUser = studentProfile?.User;
            if (offeredUser == null)
            {
                await _errorLogService.LogErrorAsync(
                    new Exception($"User not found for StudentProfile.Id = {studentId}"),
                    studentId, "EventWaitlistService.NotifyOfferedStudentAsync");
                return;
            }

            var ev = await _uow.Events.GetByIdAsync(eventId);
            if (ev == null) return;

            // Dùng offeredUser.Id (User.Id) để gửi notification
            await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
            {
                ReceiverId = offeredUser.Id,  // ✅ User.Id
                Title = "Có chỗ trống cho bạn!",
                Message = $"Có một chỗ trống vừa mở trong sự kiện '{ev.Title}'. Hãy vào đăng ký ngay!",
                Type = DataAccess.Enum.NotificationType.TicketCreated,
                RelatedEntityId = eventId
            });

            await _emailService.SendAsync(
                offeredUser.Email,
                $"[AEMS] Có chỗ trống – {ev.Title}",
                $@"<p>Xin chào <strong>{offeredUser.FullName}</strong>,</p>
               <p>Một chỗ trống vừa mở trong sự kiện <strong>{ev.Title}</strong>
                  lúc <strong>{ev.StartTime:HH:mm, dd/MM/yyyy}</strong>.</p>
               <p>Hãy đăng ký ngay trước khi hết chỗ!</p>"
            );
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(ex, studentId,
                "EventWaitlistService.NotifyOfferedStudentAsync");
        }
    }

    public async Task RespondToOfferAsync(RespondOfferRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrEmpty(dto.EventId) || string.IsNullOrEmpty(dto.StudentId))
            throw new InvalidOperationException("EventId và StudentId không được để trống.");

        var entry = await _uow.EventWaitlist.GetAsync(
            w => w.EventId == dto.EventId && w.StudentId == dto.StudentId);
        if (entry == null) throw new InvalidOperationException("Không tìm thấy entry trong waitlist.");

        var now = DateTimeHelper.GetVietnamTime();
        entry.RespondedAt = now;
        entry.UpdatedAt = now;

        if (dto.Accept)
        {
            entry.Status = DataAccess.Enum.EventWaitlistStatusEnum.Accepted;
            await _uow.EventWaitlist.UpdateAsync(entry);

            // ✅ Tạo Ticket chính thức
            var existingTicket = await _uow.Tickets.GetAsync(
                t => t.EventId == dto.EventId && t.StudentId == dto.StudentId);

            DataAccess.Entities.Ticket ticket; // ← khai báo ngoài if/else

            if (existingTicket != null)
            {
                existingTicket.Status = DataAccess.Enum.TicketStatusEnum.Registered;
                existingTicket.DeletedAt = null;
                existingTicket.UpdatedAt = now;
                await _uow.Tickets.UpdateAsync(existingTicket);
                ticket = existingTicket; // ← gán vào
            }
            else
            {
                ticket = new DataAccess.Entities.Ticket // ← gán vào, bỏ var
                {
                    Id = Guid.NewGuid().ToString(),
                    EventId = dto.EventId,
                    StudentId = dto.StudentId,
                    Status = DataAccess.Enum.TicketStatusEnum.Registered,
                    TicketCode = $"TK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _uow.Tickets.CreateAsync(ticket);
            }

            await _uow.SaveChangesAsync();
            try
            {
                var studentProfile = await _uow.StudentProfiles.GetAsync(
                    s => s.Id == dto.StudentId,
                    q => q.Include(x => x.User));

                var ev = await _uow.Events.GetAsync(
                    e => e.Id == dto.EventId,
                    q => q.Include(x => x.Location));

                if (studentProfile?.User != null && ev != null)
                {
                    // In-app notification
                    await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
                    {
                        ReceiverId = studentProfile.User.Id,
                        Title = "Đăng ký thành công!",
                        Message = $"Bạn đã xác nhận tham gia sự kiện '{ev.Title}'. Vui lòng kiểm tra email để nhận mã QR.",
                        Type = DataAccess.Enum.NotificationType.TicketCreated,
                        RelatedEntityId = dto.EventId
                    });

                    // Email kèm QR
                    string qrCodeBase64 = BusinessLogic.Helper.QRCodeGeneratorHelper
                        .GenerateQRCodeBase64(ticket.Id);
                    string locationName = ev.Location?.Name ?? ev.LocationId ?? "N/A";

                    await _emailService.SendEventRegistrationEmailAsync(
                        studentProfile.User.Email,
                        studentProfile.User.FullName ?? studentProfile.User.Email ?? "Sinh viên",
                        ev.Title,
                        ev.StartTime,
                        locationName,
                        qrCodeBase64
                    );
                }
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(ex, dto.StudentId,
                    "EventWaitlistService.RespondToOfferAsync (SendQREmail)");
            }
        }
        else
        {
            entry.Status = DataAccess.Enum.EventWaitlistStatusEnum.Cancelled;
            await _uow.EventWaitlist.UpdateAsync(entry);
            await _uow.SaveChangesAsync();

            // ✅ Tự động offer người tiếp theo
            await OfferNextAsync(dto.EventId);
        }
    }
}
