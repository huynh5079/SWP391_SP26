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

        [HttpGet]
        public async Task<IActionResult> ClickAndRoute(string notiId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var noti = await _uow.Notifications.GetAsync(n => n.Id == notiId && n.UserId == CurrentUserId && n.DeletedAt == null);
            if (noti == null) return NotFound();

            // Mark as read
            if (noti.IsRead == false)
            {
                noti.IsRead = true;
                await _uow.Notifications.UpdateAsync(noti);
                await _uow.SaveChangesAsync();
            }

            // Redirect based on NotificationType
            if (System.Enum.TryParse<DataAccess.Enum.NotificationType>(noti.Type, out var typeEnum))
            {
                switch (typeEnum)
                {
                    case DataAccess.Enum.NotificationType.SystemError:
                        return RedirectToAction("Index", "SystemLog");

                    case DataAccess.Enum.NotificationType.TicketCreated:
                    case DataAccess.Enum.NotificationType.EventCancel:
                        // Routing to MyEvents where students can see their registered/cancelled tickets
                        if (!string.IsNullOrEmpty(noti.RelatedEntityId))
                            return RedirectToAction("Detail", "StudentEvent", new { id = noti.RelatedEntityId });
                        return RedirectToAction("MyEvents", "StudentEvent");

                    case DataAccess.Enum.NotificationType.EventOrganizeCancel:
                    case DataAccess.Enum.NotificationType.EventUpdated:
                        // Routing to the details of the event
                        return RedirectToAction("Detail", "StudentEvent", new { id = noti.RelatedEntityId }); 

                    case DataAccess.Enum.NotificationType.EventApproved:
                    case DataAccess.Enum.NotificationType.EventPublished:
                    case DataAccess.Enum.NotificationType.EventRejected:
                    case DataAccess.Enum.NotificationType.EventChangeRequested:
                    case DataAccess.Enum.NotificationType.EventFeedback:
                        // Assuming Event/Details
                        return RedirectToAction("Details", "Event", new { id = noti.RelatedEntityId });
                        
                    case DataAccess.Enum.NotificationType.UserRegistered:
                    case DataAccess.Enum.NotificationType.AccountBan:
                    case DataAccess.Enum.NotificationType.AccountUnban:
                        // Assuming User/Profile
                        return RedirectToAction("Profile", "User", new { userId = noti.RelatedEntityId });
                        
                    case DataAccess.Enum.NotificationType.SystemBroadcast:
                        return RedirectToAction("Index", "Home");
                }
            }

            // Default fallback
            return RedirectToAction("Index", "Home");
        }
    }
}
