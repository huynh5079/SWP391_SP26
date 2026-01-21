using BusinessLogic.Helper;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Common
{
    public class BaseController : Controller
    {
        protected string? CurrentUserId => User.GetUserId();

        protected void SetNotification(string message, string type = "success")
        {
            TempData["NotificationMessage"] = message;
            TempData["NotificationType"] = type; // success, error, warning, info
        }
    }
}
