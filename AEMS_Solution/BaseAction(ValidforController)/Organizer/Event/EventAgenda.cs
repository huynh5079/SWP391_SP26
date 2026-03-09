using AEMS_Solution.BaseAction_ValidforController_.Organizer.Event.InterfaceEvent;
using AEMS_Solution.Models.Event.EventAgenda;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.BaseAction_ValidforController_.Organizer.Event
{
	public class EventAgendaAction : IEventAgendaAction
	{
		private readonly IUnitOfWork _unitOfWork;

		public EventAgendaAction(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public string EnsureOrganizerId(string? userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ.");
			}

			var staffProfile = _unitOfWork.StaffProfiles
				.GetAsync(x => x.UserId == userId && x.DeletedAt == null)
				.GetAwaiter()
				.GetResult();
			if (staffProfile == null)
			{
				throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile) cho tài khoản này.");
			}

			return staffProfile.Id;
		}

		public async Task<DataAccess.Entities.EventAgenda> EnsureAgendaOwnershipAsync(string organizerId, string agendaId)
		{
			if (string.IsNullOrWhiteSpace(agendaId))
			{
				throw new KeyNotFoundException("Agenda không hợp lệ.");
			}

			var agenda = await _unitOfWork.EventAgenda.GetAsync(
				x => x.Id == agendaId && x.DeletedAt == null,
				q => q.Include(x => x.Event));

			if (agenda == null || agenda.Event == null || agenda.Event.DeletedAt != null)
			{
				throw new KeyNotFoundException("Không tìm thấy agenda.");
			}

			if (!string.Equals(agenda.Event.OrganizerId, organizerId, StringComparison.Ordinal))
			{
				throw new UnauthorizedAccessException("Bạn không có quyền quản lý agenda này.");
			}

			return agenda;
		}

		public void EnsureValidAgenda(EditAgendaViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.SessionName))
			{
				throw new InvalidOperationException("Vui lòng nhập Session Name.");
			}

			if (string.IsNullOrWhiteSpace(model.SpeakerName))
			{
				throw new InvalidOperationException("Vui lòng nhập Speaker.");
			}

			if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime <= model.StartTime)
			{
				throw new InvalidOperationException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
			}
		}
	}
}
