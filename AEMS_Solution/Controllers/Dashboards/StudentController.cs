using AEMS_Solution.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Student")]
    public class StudentController : BaseController
    {
        public IActionResult Index()
        {
            // Redirect straight to the event calendar — the student's home page
            return RedirectToAction("Index", "StudentEvent");
        }
        [HttpGet]
        public async Task <IActionResult> ShowQuiz(){
			return RedirectToAction("ShowQuiz", "EventQuiz");
		}
	}
}
