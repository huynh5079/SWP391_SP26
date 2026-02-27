using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Service.Interfaces;

namespace BusinessLogic.Service.InterfaceforOrganizer
{
	public interface IOrganizerService : IDashboardService,IEventService,IDropdownService
	{
	}
}
