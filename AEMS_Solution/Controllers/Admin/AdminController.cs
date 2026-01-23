using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
