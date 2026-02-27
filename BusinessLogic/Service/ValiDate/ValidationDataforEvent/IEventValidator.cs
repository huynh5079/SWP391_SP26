using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.ValiDate.ValidationDataforEvent
{
	public interface IEventValidator
	{
		 void ValidateCreate(CreateEventRequestDto dto);
		 void ValidateUpdate(UpdateEventRequestDto dto);
		 // Validate agendas (times / required fields)
		 void ValidateAgendas(List<CreateAgendaItemDto>? agendas);
		 // Validate deposit rules
		 void ValidateDeposit(CreateEventRequestDto dto);

	}
}
