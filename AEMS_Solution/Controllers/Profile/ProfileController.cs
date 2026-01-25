using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Profile
{
    public class ProfileController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
