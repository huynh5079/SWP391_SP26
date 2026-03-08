using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Ticket;

namespace BusinessLogic.Service.Event.Sub_Service.Ticket
{
	 public interface ITicketService
	{
		Task<List<TicketDTO>> GetAllTicketsAsync();

		Task<TicketDTO?> GetTicketByIdAsync(string ticketId);

		Task<List<TicketDTO>> GetTicketsByEventAsync(string eventId);

		Task<List<TicketDTO>> GetTicketsByStudentAsync(string studentId);

		Task<TicketDTO> CreateTicketAsync(CreateTicketDTO dto);

		Task<bool> UpdateTicketAsync(string ticketId, UpdateTicketDTO dto);
	}
}
