using AEMS_Solution.Controllers.Common;
using BusinessLogic.Service.System;
using BusinessLogic.Service.UserActivities;
using DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly DataAccess.Repositories.Abstraction.IUnitOfWork _uow;
        public AdminController(DataAccess.Repositories.Abstraction.IUnitOfWork uow)
        {
            _uow = uow;
        }

        private ISystemErrorLogService _logService => HttpContext.RequestServices.GetRequiredService<ISystemErrorLogService>();
        private IUserActivityLogService _activityService => HttpContext.RequestServices.GetRequiredService<IUserActivityLogService>();
        private INotificationService _notificationService => HttpContext.RequestServices.GetRequiredService<INotificationService>();

        public async Task<IActionResult> Index()
        {
            try 
            {
                // 1. Error Stats — 7-day trend
                var (dates, counts, todayErrors) = await _logService.GetErrorTrendAsync(7);

                // 2. Error Stats — 30-day total for sidebar badge + stats
                var (dates30, counts30, _) = await _logService.GetErrorTrendAsync(30);
                int total30Days = counts30.Sum();

                // 3. User Stats
                var allUsers = await _uow.Users.GetAllAsync(null, query => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(query, u => u.Role));
                var totalUsers    = allUsers.Count();
                var totalStudents = allUsers.Count(u => u.Role != null && u.Role.RoleName == DataAccess.Enum.RoleEnum.Student);
                var totalStaff    = allUsers.Count(u => u.Role != null && (u.Role.RoleName == DataAccess.Enum.RoleEnum.Organizer || u.Role.RoleName == DataAccess.Enum.RoleEnum.Approver));
                
                var userDist = new Dictionary<string, int>
                {
                    { "Student", totalStudents },
                    { "Staff",   totalStaff },
                    { "Admin",   allUsers.Count(u => u.Role != null && u.Role.RoleName == DataAccess.Enum.RoleEnum.Admin) }
                };

                var model = new Models.Admin.AdminDashboardViewModel
                {
                    TotalUsers              = totalUsers,
                    TotalStudents           = totalStudents,
                    TotalStaff              = totalStaff,
                    TotalErrorsToday        = todayErrors,
                    TotalErrorsLast30Days   = total30Days,
                    ErrorTrendData          = counts,
                    ErrorTrendLabels        = dates,
                    UserDistribution        = userDist,
                    RecentActivities        = (await _activityService.GetRecentActivitiesAsync(10)).ToList(),
                    RecentNotifications     = (await _notificationService.GetUserNotificationsAsync(CurrentUserId ?? "")).Take(10).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, "Failed to load dashboard data: " + ex.Message);
                return View(new Models.Admin.AdminDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ActivityLog(int page = 1, int pageSize = 20, string? search = null)
        {
            try
            {
                var result = await _activityService.GetLogsAsync(page, pageSize, search);
                return View(result);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, "Failed to load activity logs.");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOldLogs(int days = 30)
        {
            try
            {
                await _activityService.DeleteOldLogsAsync(days);
                TempData["SuccessMessage"] = $"Old logs (older than {days} days) have been cleaned up.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to clean up logs: " + ex.Message;
            }
            return RedirectToAction("ActivityLog");
        }
    }
}
