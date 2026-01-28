using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.Profile
{
    public class ProfileController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
