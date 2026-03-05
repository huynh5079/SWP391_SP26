using BusinessLogic.DTOs.System;
using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.Notification
{
    // Restrict this controller to Admin only
    [Authorize(Roles = "Admin")]
    public class AdminNotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _uow;

        public AdminNotificationController(INotificationService notificationService, IUnitOfWork uow)
        {
            _notificationService = notificationService;
            _uow = uow;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/AdminNotification/Create.cshtml", new BroadcastNotificationDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BroadcastNotificationDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/AdminNotification/Create.cshtml", model);
            }

            try
            {
                List<string> targetUserIds = new List<string>();

                switch (model.TargetGroup)
                {
                    case BroadcastTargetGroup.AllSystem:
                        var allUsers = await _uow.Users.GetAllAsync(u => u.DeletedAt == null && u.Status == UserStatusEnum.Active);
                        targetUserIds = allUsers.Select(u => u.Id).ToList();
                        break;
                    
                    case BroadcastTargetGroup.ByRole:
                        if (model.TargetRole == null)
                        {
                            ModelState.AddModelError("TargetRole", "Please select a role.");
                            return View("~/Views/AdminNotification/Create.cshtml", model);
                        }
                        // Compare enum values directly (both sides are RoleEnum?)
                        var targetRoleVal = model.TargetRole;
                        var roleUsers = await _uow.Users.GetAllAsync(u =>
                            u.Role != null &&
                            u.Role.RoleName == targetRoleVal &&
                            u.DeletedAt == null &&
                            u.Status == UserStatusEnum.Active);
                        targetUserIds = roleUsers.Select(u => u.Id).ToList();
                        break;

                    case BroadcastTargetGroup.SpecificEmail:
                        if (string.IsNullOrWhiteSpace(model.SpecificEmail))
                        {
                            ModelState.AddModelError("SpecificEmail", "Please enter the recipient's email.");
                            return View("~/Views/AdminNotification/Create.cshtml", model);
                        }
                        var emailUser = await _uow.Users.GetAsync(u => u.Email == model.SpecificEmail.Trim());
                        if (emailUser == null)
                        {
                            ModelState.AddModelError("SpecificEmail", "No user found with that email address.");
                            return View("~/Views/AdminNotification/Create.cshtml", model);
                        }
                        targetUserIds.Add(emailUser.Id);
                        break;
                }

                if (!targetUserIds.Any())
                {
                    TempData["ErrorMessage"] = "No users found matching the selected target group.";
                    return View("~/Views/AdminNotification/Create.cshtml", model);
                }

                // Fire notification for everyone in the target list
                foreach (var userId in targetUserIds)
                {
                    // Optionally we could offload this to a background job if targetUserIds is massive.
                    await _notificationService.SendNotificationAsync(
                        userId,
                        $"[SYSTEM] {model.Title}",
                        model.Message,
                        "SystemBroadcast"
                    );
                }

                TempData["SuccessMessage"] = $"Notification sent successfully to {targetUserIds.Count} user(s).";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while sending the notification: " + ex.Message;
                return View("~/Views/AdminNotification/Create.cshtml", model);
            }
        }
    }
}
