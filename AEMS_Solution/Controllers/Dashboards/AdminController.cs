using AEMS_Solution.Controllers.Common;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly DataAccess.Repositories.Abstraction.IUnitOfWork _uow;
        private readonly ISystemErrorLogService _logService;

        public AdminController(DataAccess.Repositories.Abstraction.IUnitOfWork uow, ISystemErrorLogService logService)
        {
            _uow = uow;
            _logService = logService;
        }

        public async Task<IActionResult> Index()
        {
            try 
            {
                // 1. Error Stats
                var (dates, counts, todayErrors) = await _logService.GetErrorTrendAsync(7);

                // 2. User Stats
                // Note: Fetching all users is heavy. Should add Count methods to Repo later.
                var allUsers = await _uow.Users.GetAllAsync();
                var totalUsers = allUsers.Count();
                var totalStudents = allUsers.Count(u => u.Role != null && u.Role.RoleName == DataAccess.Enum.RoleEnum.Student);
                var totalStaff = allUsers.Count(u => u.Role != null && (u.Role.RoleName == DataAccess.Enum.RoleEnum.Organizer || u.Role.RoleName == DataAccess.Enum.RoleEnum.Approver));
                
                // Get Admin Count too?
                // Distribution
                var userDist = new Dictionary<string, int>
                {
                    { "Student", totalStudents },
                    { "Staff", totalStaff },
                    { "Admin", allUsers.Count(u => u.Role != null && u.Role.RoleName == DataAccess.Enum.RoleEnum.Admin) }
                };

                var model = new Models.Admin.AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalStudents = totalStudents,
                    TotalStaff = totalStaff,
                    TotalErrorsToday = todayErrors,
                    ErrorTrendData = counts,
                    ErrorTrendLabels = dates,
                    UserDistribution = userDist
                };

                return View(model);
            }
            catch (Exception ex)
            {
                SetError("Failed to load dashboard data: " + ex.Message);
                return View(new Models.Admin.AdminDashboardViewModel());
            }
        }
    }
}
