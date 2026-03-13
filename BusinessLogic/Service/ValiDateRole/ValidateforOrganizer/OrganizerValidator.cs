using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Ticket;
using DataAccess.Entities;
using DataAccess.Enum;

namespace BusinessLogic.Service.ValiDateRole.ValidateforOrganizer
{
	public class OrganizerValidator : IOrganizerValidator
	{
		public void ValidateCheckInRequest(CheckInRequestDto request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
			{
				throw new BusinessValidationException("Dữ liệu QR Code rỗng hoặc không hợp lệ.");
			}
		}

		public void ValidateOrganizerCanCheckIn(StaffProfile? organizerProfile)
		{
			if (organizerProfile == null)
			{
				throw new BusinessValidationException("Chỉ ban tổ chức mới có quyền check-in.");
			}
		}

		public void ValidateTicketForCheckIn(Ticket? ticket)
		{
			if (ticket == null || ticket.DeletedAt != null || ticket.Status == TicketStatusEnum.Cancelled)
			{
				throw new BusinessValidationException("Vé không tồn tại hoặc đã bị hủy.");
			}
		}

		public void ValidateEventOwnership(Ticket ticket, StaffProfile organizerProfile)
		{
			if (ticket.Event?.OrganizerId != organizerProfile.Id)
			{
				throw new BusinessValidationException("Sự kiện này không thuộc quyền quản lý của bạn.");
			}
		}

		public void ValidateNotAlreadyCheckedIn(Ticket ticket)
		{
			if (ticket.Status == TicketStatusEnum.CheckedIn)
			{
				throw new BusinessValidationException("Học sinh này đã được Check-in trước đó rồi.");
			}
		}

		public void ValidateCheckInWindow(Ticket ticket, DateTime currentTime)
		{
			if (ticket.Event == null)
			{
				throw new BusinessValidationException("Không tìm thấy thông tin sự kiện để kiểm tra thời gian check-in.");
			}

			if (currentTime < ticket.Event.StartTime.AddDays(-1) || currentTime > ticket.Event.EndTime.AddHours(2))
			{
				throw new BusinessValidationException("Khoảng thời gian này không được phép Check-in.");
			}
		}

		public class BusinessValidationException : Exception
		{
			public BusinessValidationException(string message) : base(message)
			{
			}
		}
	}
}
