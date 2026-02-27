using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
	public class ApprovalController : BaseController
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
