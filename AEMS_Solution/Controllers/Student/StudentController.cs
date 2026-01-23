using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Student
{
    [Authorize(Roles = "Student")]
    public class StudentController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
