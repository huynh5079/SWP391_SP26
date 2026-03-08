using BusinessLogic.DTOs.Event.Ticket;
using DataAccess.Entities;

namespace BusinessLogic.Service.ValidationData.Ticket
{
	public interface ITicketValidator
	{
		void ValidateCreateRequest(CreateTicketDTO dto);
		void ValidateEventExists(DataAccess.Entities.Event? eventEntity);
		void ValidateStudentExists(StudentProfile? student);
		void ValidateDuplicateActiveTicket(DataAccess.Entities.Ticket? existingTicket);
		void ValidateUpdateRequest(string ticketId);
		void ValidateTicketExists(DataAccess.Entities.Ticket? ticket);
	}
}
