using BusinessLogic.Helper;
using BusinessLogic.Service.System;
using BusinessLogic.Service.UserActivities;
using BusinessLogic.DTOs;
using DataAccess.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_Solution.Controllers.Common
{
    /// <summary>
    /// Base Controller providing global access to Logging and Notifications 
    /// without requiring constructor injection in derived classes.
    /// </summary>
    public class BaseController : Controller
    {
        private IUserActivityLogService? _userActivityLogService;
        private ISystemErrorLogService? _systemErrorLogService;
        private INotificationService? _notificationService;

        protected string? CurrentUserId => User.GetUserId();

        // Lazy properties using Service Locator pattern for convenience in all controllers
        protected IUserActivityLogService UserActivityLogService => 
            _userActivityLogService ??= HttpContext.RequestServices.GetRequiredService<IUserActivityLogService>();

        protected ISystemErrorLogService SystemErrorLogService => 
            _systemErrorLogService ??= HttpContext.RequestServices.GetRequiredService<ISystemErrorLogService>();

        protected INotificationService NotificationService => 
            _notificationService ??= HttpContext.RequestServices.GetRequiredService<INotificationService>();

        #region User Notifications (TempData/Toasts)
        protected void SetNotification(string message, string type = "success")
        {
            TempData["NotificationMessage"] = message;
            TempData["NotificationType"] = type; // success, error, warning, info
        }

        protected void SetSuccess(string message) => SetNotification(message, "success");
        protected void SetError(string message) => SetNotification(message, "error");
        protected void SetWarning(string message) => SetNotification(message, "warning");
        protected void SetInfo(string message) => SetNotification(message, "info");
        #endregion

        #region Global Unified Helpers (Unified Activity + Toast + Notification)
        /// <summary>
        /// Handles a successful operation: logs activity, sends a system notification, and sets UI success message.
        /// </summary>
        protected async Task ExecuteSuccessAsync(string message, UserActionType actionType, string? targetId = null, TargetType targetType = TargetType.None, string? notifyRecipient = null)
        {
            SetSuccess(message);
            await LogUserActivity(actionType, targetId, targetType, message);
            var recipient = notifyRecipient ?? CurrentUserId;
            if (!string.IsNullOrEmpty(recipient))
            {
                await SendSystemNotification(recipient, message);
            }
        }

        /// <summary>
        /// Handles an error: logs system exception and sets UI error message.
        /// </summary>
        protected async Task ExecuteErrorAsync(Exception ex, string? uiMessage = null)
        {
            SetError(uiMessage ?? "Đã xảy ra lỗi không mong đợi.");
            await LogSystemError(ex, uiMessage);
        }
        #endregion

        #region Global Logging Helpers (Original)
        /// <summary>
        /// Logs a user activity (e.g., Created Event, Updated Profile).
        /// </summary>
        protected async Task LogUserActivity(UserActionType actionType, string? targetId = null, TargetType targetType = TargetType.None, string? description = null)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return;
            await UserActivityLogService.LogActivityAsync(CurrentUserId, actionType, targetId, targetType, description);
        }

        /// <summary>
        /// Logs a system error with correlation to the current user if available.
        /// </summary>
        protected async Task LogSystemError(Exception ex, string? customMessage = null)
        {
            await SystemErrorLogService.LogErrorAsync(ex, customMessage, CurrentUserId);
        }

        /// <summary>
        /// Sends a persistent system-wide notification (Database + Real-time).
        /// </summary>
        protected async Task SendSystemNotification(string userId, string message, NotificationType type = NotificationType.SystemBroadcast, string? relatedId = null)
        {
            await NotificationService.SendNotificationAsync(new SendNotificationRequest
            {
                ReceiverId = userId,
                Title = "Hệ thống thông báo",
                Message = message,
                Type = type,
                RelatedEntityId = relatedId
            });
        }
        #endregion
    }
}
