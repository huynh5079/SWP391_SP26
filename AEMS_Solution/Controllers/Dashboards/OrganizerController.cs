using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Staff")]
    public class OrganizerController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
