using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Service.Dashboard;
using BusinessLogic.Service.Event;

namespace BusinessLogic.Service.Organizer
{
	public interface IOrganizerService : IDashboardService,IEventService,IDropdownService,IEventWaitlistService
	{
	}
}
