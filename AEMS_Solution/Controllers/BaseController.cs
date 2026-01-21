using BusinessLogic.Helper;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers
{
    public class BaseController : Controller
    {
        protected string? CurrentUserId => User.GetUserId();

        protected void NotifySuccess(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void NotifyError(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}
