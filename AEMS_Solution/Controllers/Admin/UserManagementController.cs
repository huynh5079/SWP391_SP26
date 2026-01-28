using BusinessLogic.DTOs;
using BusinessLogic.Service.Interface;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly IUserService _userService;

        public UserManagementController(IUserService userService)
        {
            _userService = userService;
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
                TempData["Success"] = "User status updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
