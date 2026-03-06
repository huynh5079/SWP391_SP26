using BusinessLogic.DTOs;
using BusinessLogic.Service.User;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.UserManagement
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly IUserService _userService;
        private readonly BusinessLogic.Service.Auth.IAuthService _authService;

        public UserManagementController(IUserService userService, BusinessLogic.Service.Auth.IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? searchTerm = null, 
            string? role = null, 
            UserStatusEnum? status = null)
        {
            var result = await _userService.GetUsersAsync(pageNumber, pageSize, searchTerm, role, status);
            
            // Pass filter values to View via ViewBag or separate ViewModel if needed
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userService.GetUserDetailAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Ban(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var result = await _userService.ToggleBanUserAsync(id);
            if (!result)
            {
                TempData["Error"] = "Failed to update user status.";
            }
            else
            {
                // Get updated state to show correct message
                var user = await _userService.GetUserDetailAsync(id);
                var isBanned = user?.IsBanned == true;
                TempData["Success"] = isBanned 
                    ? "Đã khóa tài khoản thành công." 
                    : "Đã mở khóa tài khoản thành công.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View(new BusinessLogic.DTOs.Authentication.Register.RegisterStaffRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(BusinessLogic.DTOs.Authentication.Register.RegisterStaffRequestDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _authService.RegisterStaffAsync(model);
                TempData["Success"] = $"Tạo tài khoản {model.RoleName} thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Note: Better to use generic error logging, but exposing message for now to admin
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
    }
}
