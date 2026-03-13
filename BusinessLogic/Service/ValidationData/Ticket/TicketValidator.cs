using BusinessLogic.DTOs.Event.Ticket;
using DataAccess.Entities;
using DataAccess.Enum;

namespace BusinessLogic.Service.ValidationData.Ticket
{
	public class TicketValidator : ITicketValidator
	{
		public void ValidateCreateRequest(CreateTicketDTO dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.EventId) || string.IsNullOrWhiteSpace(dto.StudentId))
			{
				throw new BusinessValidationException("EventId và StudentId là bắt buộc.");
			}
		}

		public void ValidateEventExists(DataAccess.Entities.Event? eventEntity)
		{
			if (eventEntity == null)
			{
				throw new BusinessValidationException("Sự kiện không tồn tại.");
			}
		}

		public void ValidateStudentExists(StudentProfile? student)
		{
			if (student == null)
			{
				throw new BusinessValidationException("Sinh viên không tồn tại.");
			}
		}

		public void ValidateDuplicateActiveTicket(DataAccess.Entities.Ticket? existingTicket)
		{
			if (existingTicket != null && existingTicket.Status != TicketStatusEnum.Cancelled)
			{
				throw new BusinessValidationException("Sinh viên đã có vé cho sự kiện này.");
			}
		}

		public void ValidateUpdateRequest(string ticketId)
		{
			if (string.IsNullOrWhiteSpace(ticketId))
			{
				throw new BusinessValidationException("TicketId không hợp lệ.");
			}
		}

		public void ValidateTicketExists(DataAccess.Entities.Ticket? ticket)
		{
			if (ticket == null)
			{
				throw new BusinessValidationException("Vé không tồn tại.");
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
