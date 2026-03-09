using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Ticket;
using DataAccess.Entities;

namespace BusinessLogic.Service.ValiDateRole.ValidateforOrganizer
{
	public interface IOrganizerValidator
	{
		void ValidateCheckInRequest(CheckInRequestDto request);
		void ValidateOrganizerCanCheckIn(StaffProfile? organizerProfile);
		void ValidateTicketForCheckIn(Ticket? ticket);
		void ValidateEventOwnership(Ticket ticket, StaffProfile organizerProfile);
		void ValidateNotAlreadyCheckedIn(Ticket ticket);
		void ValidateCheckInWindow(Ticket ticket, DateTime currentTime);
	}
}
