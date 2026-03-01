using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;

namespace BusinessLogic.Service.ValidationData
{
	public interface IEventValidator
	{
		 void ValidateCreate(CreateEventRequestDto dto);
		 void ValidateUpdate(UpdateEventRequestDto dto);
	}
}
