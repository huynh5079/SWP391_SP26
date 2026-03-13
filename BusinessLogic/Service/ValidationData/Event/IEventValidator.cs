using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.ValidationData.Event
{
	public interface IEventValidator
	{
		 void ValidateCreate(CreateEventRequestDto dto);
		 void ValidateUpdate(UpdateEventRequestDto dto);
		 void ValidateAgendas(List<CreateAgendaItemDto>? agendas);
		 void ValidateDeposit(CreateEventRequestDto dto);
	}
}
