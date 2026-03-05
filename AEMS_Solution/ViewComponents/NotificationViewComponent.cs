using BusinessLogic.DTOs;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AEMS_Solution.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _uow;

        public NotificationViewComponent(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Not logged in or cant resolve ID
                return View(new List<DataAccess.Entities.Notification>());
            }

            // Fetch top 5 unread notifications for display in the bell dropdown
            var notifications = await _uow.Notifications.GetAllAsync(
                n => n.UserId == userId && n.DeletedAt == null);


            var topUnread = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            ViewBag.UnreadCount = notifications.Count(n => n.IsRead == false);

            return View(topUnread);
        }
    }
}
