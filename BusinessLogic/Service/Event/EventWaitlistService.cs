using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Entities;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Event;

public class EventWaitlistService : IEventWaitlistService
{
	private readonly IUnitOfWork _uow;

	public EventWaitlistService(IUnitOfWork uow)
	{
		_uow = uow;
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

		var next = (await _uow.EventWaitlist.GetAllAsync(w => w.EventId == eventId && (w.Status == DataAccess.Enum.EventWaitlistStatusEnum.Waiting || w.Status == null),
			q => q.OrderBy(x => x.Position))).FirstOrDefault();

		if (next == null) return;

		var now = DateTimeHelper.GetVietnamTime();
		next.Status = DataAccess.Enum.EventWaitlistStatusEnum.Offered;
		next.OfferedAt = now;
		next.IsNotified = true;
		next.UpdatedAt = now;

		await _uow.EventWaitlist.UpdateAsync(next);
		await _uow.SaveChangesAsync();

		// TODO: push notification/email to student
	}

	public async Task RespondToOfferAsync(RespondOfferRequestDto dto)
	{
		if (dto == null) throw new ArgumentNullException(nameof(dto));
		if (string.IsNullOrEmpty(dto.EventId) || string.IsNullOrEmpty(dto.StudentId))
			throw new InvalidOperationException("EventId và StudentId không được để trống.");

		var entry = await _uow.EventWaitlist.GetAsync(w => w.EventId == dto.EventId && w.StudentId == dto.StudentId);
		if (entry == null) throw new InvalidOperationException("Không tìm thấy entry trong waitlist.");

		var now = DateTimeHelper.GetVietnamTime();
		entry.RespondedAt = now;
		entry.UpdatedAt = now;

		if (dto.Accept)
		{
			entry.Status = DataAccess.Enum.EventWaitlistStatusEnum.Accepted;
			// Optionally: create ticket here. For now only update status.
		}
		else
		{
			entry.Status = DataAccess.Enum.EventWaitlistStatusEnum.Cancelled;
		}

		await _uow.EventWaitlist.UpdateAsync(entry);
		await _uow.SaveChangesAsync();
	}
}
