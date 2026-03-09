using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Approval
{
	public class ApproverService : IApproverCommandService, IApproverQueryService
	{
		private readonly IUnitOfWork _uow;
		private readonly INotificationService _notificationService;

		public ApproverService(IUnitOfWork uow, INotificationService notificationService)
		{
			_uow = uow;
			_notificationService = notificationService;
		}
		public async Task<List<EventItemDto>> GetPendingEventsAsync(string? search, string? status, int page = 1, int pageSize = 10)
		{
			if (page <= 0) page = 1;
			if (pageSize <= 0) pageSize = 10;

			// NOTE: Repository của bạn có thể khác.
			// Mình assume _uow.Events.Query() trả về IQueryable<Event>.
			// Nếu không có Query(), bạn đổi sang method tương đương GetAllAsync + include.
			var list = await _uow.Events.GetAllAsync( e => e.Status == EventStatusEnum.Pending,
				q => q.Include(x => x.ApprovalLogs));

			if (!string.IsNullOrWhiteSpace(search))
			{
				search = search.Trim();
				list = list.Where(e => e.Title.Contains(search)).ToList();
			}

			list = list
				.OrderByDescending(e => e.UpdatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			var items = list.Select(e =>
			{
				var lastLog = e.ApprovalLogs?
					.Where(l => l.DeletedAt == null)
					.OrderByDescending(l => l.CreatedAt)
					.FirstOrDefault();

				return new EventItemDto
				{
					Id = e.Id,
					Title = e.Title,
					StartTime = e.StartTime,
					EndTime = e.EndTime,
					Status = e.Status,
					ThumbnailUrl = e.ThumbnailUrl,
					LastApprovalComment = lastLog?.Comment
				};
			}).ToList();

			return items;

		}

		// =========================
		// QUERY: Logs of a specific event
		// =========================
		public async Task<List<ApprovalLogDto>> GetApprovalLogsAsync(string eventId)
		{
			// 1) Lấy toàn bộ log (hoặc nếu repo có overload filter thì dùng filter luôn)
			var list = await _uow.EventApprovalLogs.GetAllAsync(l => l.EventId == eventId && l.DeletedAt == null); ;

			// 2) Lọc + sort + map trong memory
			var logs = list
				.Where(l => l.EventId == eventId && l.DeletedAt == null)
				.OrderByDescending(l => l.CreatedAt)
				.Select(l => new ApprovalLogDto
				{
					EventId = l.EventId,
					ApproverId = l.ApproverId ?? "",
					Action = l.Action,
					Comment = l.Comment,
					CreatedAt = l.CreatedAt
				})
				.ToList();

			return logs;
		}

		// =========================
		// COMMAND: Approve
		// =========================
		public async Task ApproveAsync(string eventId, string approverId, string? comment)
		{
			var ev = await _uow.Events.GetAsync(e => e.Id == eventId,q => q.Include(e => e.ApprovalLogs));

			if (ev == null) throw new Exception("Event not found.");

			// Optional: chỉ cho approve khi đang PendingApproval
			if (ev.Status != EventStatusEnum.Pending)
				throw new Exception($"Event status must be PendingApproval to approve. Current: {ev.Status}");

			var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();

			ev.Status = EventStatusEnum.Approved;
			ev.UpdatedAt = now;

			// Map approverId (which may be AspNetUsers.Id) -> StaffProfile.Id for FK
			var approverProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == approverId);
			var approverProfileId = approverProfile?.Id;

			await _uow.EventApprovalLogs.CreateAsync(new ApprovalLog
			{
				Id = Guid.NewGuid().ToString(),
				EventId = eventId,
				ApproverId = approverProfileId,
				Action = ApprovalActionEnum.Approve,
				Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
				CreatedAt = now,
				UpdatedAt = now,
				DeletedAt = null
			});

			await _uow.SaveChangesAsync();

			// Notify Organizer
			if (ev.Organizer?.UserId != null)
			{
				await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
				{
					ReceiverId = ev.Organizer.UserId, 
					Title = "Sự kiện được duyệt", 
					Message = $"Sự kiện '{ev.Title}' của bạn đã được kiểm duyệt viên chấp thuận.", 
					Type = DataAccess.Enum.NotificationType.EventApproved,
					RelatedEntityId = ev.Id
				});
			}
		}

		// =========================
		// COMMAND: Reject
		// =========================
		public async Task RejectAsync(string eventId, string approverId, string? comment)
		{
			var ev = await _uow.Events.GetAsync(e => e.Id == eventId, q => q.Include(e => e.Organizer));

			if (ev == null) throw new Exception("Event not found.");

			if (ev.Status != EventStatusEnum.Pending)
				throw new Exception($"Event status must be Pending to reject. Current: {ev.Status}");

			if (string.IsNullOrWhiteSpace(comment))
				throw new InvalidOperationException("Vui lòng nhập lý do khi từ chối sự kiện.");

			var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();

			ev.Status = EventStatusEnum.Rejected;
			ev.UpdatedAt = now;

			// Map approverId -> StaffProfile.Id
			var approverProfileForReject = await _uow.StaffProfiles.GetAsync(s => s.UserId == approverId);
			var approverProfileForRejectId = approverProfileForReject?.Id;

			await _uow.EventApprovalLogs.CreateAsync(new ApprovalLog
			{
				Id = Guid.NewGuid().ToString(),
				EventId = eventId,
				ApproverId = approverProfileForRejectId,
				Action = ApprovalActionEnum.Reject,
				Comment = comment?.Trim(),
				CreatedAt = now,
				UpdatedAt = now,
				DeletedAt = null
			});

			await _uow.SaveChangesAsync();

			// Notify Organizer
			if (ev.Organizer?.UserId != null)
			{
				await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
				{
					ReceiverId = ev.Organizer.UserId, 
					Title = "Sự kiện bị từ chối", 
					Message = $"Sự kiện '{ev.Title}' của bạn đã bị từ chối với lý do: {comment}", 
					Type = DataAccess.Enum.NotificationType.EventRejected,
					RelatedEntityId = ev.Id
				});
			}
		}

		// =========================
		// COMMAND: Request change
		// =========================
		public async Task RequestChangeAsync(string eventId, string approverId, string? comment)
		{
			var ev = await _uow.Events.GetAsync(e => e.Id == eventId, q => q.Include(e => e.Organizer));

			if (ev == null) throw new Exception("Event not found.");

			if (ev.Status != EventStatusEnum.Pending)
				throw new Exception($"Event status must be Pending to request change. Current: {ev.Status}");

			if (string.IsNullOrWhiteSpace(comment))
				throw new InvalidOperationException("Vui lòng nhập yêu cầu chỉnh sửa khi gửi trả sự kiện.");

			var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();

			// ⚠️ Tuỳ enum EventStatusEnum của bạn:
			// - Nếu có RequestChange/PendingChanges thì set vào đó
			// - Nếu không có thì thường đưa về Draft để Organizer sửa
			ev.Status = EventStatusEnum.Draft;
			ev.UpdatedAt = now;

			// Map approverId -> StaffProfile.Id
			var approverProfileForRequest = await _uow.StaffProfiles.GetAsync(s => s.UserId == approverId);
			var approverProfileForRequestId = approverProfileForRequest?.Id;

			await _uow.EventApprovalLogs.CreateAsync(new ApprovalLog
			{
				Id = Guid.NewGuid().ToString(),
				EventId = eventId,
				ApproverId = approverProfileForRequestId,
				Action = ApprovalActionEnum.RequestChange,
				Comment = comment?.Trim(),
				CreatedAt = now,
				UpdatedAt = now,
				DeletedAt = null
			});

			await _uow.SaveChangesAsync();

			// Notify Organizer
			if (ev.Organizer?.UserId != null)
			{
				await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
				{
					ReceiverId = ev.Organizer.UserId, 
					Title = "Yêu cầu chỉnh sửa sự kiện", 
					Message = $"Sự kiện '{ev.Title}' cần được chỉnh sửa: {comment}", 
					Type = DataAccess.Enum.NotificationType.EventChangeRequested,
					RelatedEntityId = ev.Id
				});
			}
		}

		// =========================
		// Existing: detail
		// =========================
		public async Task<ApproverEventDetailDto?> GetEventDetailAsync(string eventId)
		{
			var eventDetail = await _uow.Events.GetAsync(
				e => e.Id == eventId,
				q => q.Include(e => e.Organizer).ThenInclude(sp => sp.User)
					  .Include(e => e.ApprovalLogs)
			);

			if (eventDetail == null) return null;

			return new ApproverEventDetailDto
			{
				EventId = eventDetail.Id,
				Title = eventDetail.Title,
				Description = eventDetail.Description,
				StartTime = eventDetail.StartTime,
				EndTime = eventDetail.EndTime,
				MaxCapacity = eventDetail.MaxCapacity,
				Status = eventDetail.Status,
				OrganizerId = eventDetail.OrganizerId ?? "",
				OrganizerName = eventDetail.Organizer?.User?.FullName ?? "",
				OrganizerEmail = eventDetail.Organizer?.User?.Email ?? "",
				ApprovalLogs = eventDetail.ApprovalLogs
					.Where(l => l.DeletedAt == null)
					.OrderByDescending(l => l.CreatedAt)
					.Select(log => new ApprovalLogDto
					{
						EventId = log.EventId,
						ApproverId = log.ApproverId ?? "",
						Action = log.Action, // ✅ không cần ?? default nếu DB/Entity đã NOT NULL
						Comment = log.Comment,
						CreatedAt = log.CreatedAt,
					}).ToList()
			};
		}

	}
}
