using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AEMS_Solution.Controllers.Common
{
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly IUnitOfWork _uow;

        public NotificationController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var notifications = await _uow.Notifications.GetAllAsync(
                n => n.UserId == CurrentUserId && n.DeletedAt == null);

            var ordered = notifications.OrderByDescending(n => n.CreatedAt).ToList();

            return View(ordered);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            if (CurrentUserId == null) return Unauthorized();

            var notification = await _uow.Notifications.GetAsync(
                n => n.Id == id && n.UserId == CurrentUserId && n.DeletedAt == null);

            if (notification != null && notification.IsRead == false)
            {
                notification.IsRead = true;
                await _uow.Notifications.UpdateAsync(notification);
                await _uow.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (CurrentUserId == null) return Unauthorized();

            var unread = await _uow.Notifications.GetAllAsync(
                n => n.UserId == CurrentUserId && n.IsRead == false && n.DeletedAt == null);

            if (unread.Any())
            {
                foreach (var item in unread)
                {
                    item.IsRead = true;
                    await _uow.Notifications.UpdateAsync(item);
                }
                await _uow.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
