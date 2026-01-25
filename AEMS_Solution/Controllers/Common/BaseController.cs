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

        protected void SetSuccess(string message)
        {
            SetNotification(message, "success");
        }

        protected void SetError(string message)
        {
            SetNotification(message, "error");
        }

        protected void SetWarning(string message)
        {
            SetNotification(message, "warning");
        }
        
        protected void SetInfo(string message)
        {
            SetNotification(message, "info");
        }
    }
}
