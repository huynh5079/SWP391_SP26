using BusinessLogic.DTOs;
using BusinessLogic.DTOs.Role;
using BusinessLogic.Service.User;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;

using AEMS_Solution.Controllers.Common;

namespace AEMS_Solution.Controllers.Features.UserManagement
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : BaseController
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
        public async Task<IActionResult> Ban(AdminSoftDeleteLimitDTO request)
        {
            if (string.IsNullOrEmpty(request.Id)) return BadRequest();

            try
            {
                var result = await _userService.SetUserLockAsync(request);
                if (!result)
                {
                    await ExecuteErrorAsync(new Exception("Failed to update user status."), "Failed to update user status.");
                }
                else
                {
                    var msg = request.ReactivateAt.HasValue
                        ? $"Đã khóa tài khoản đến {request.ReactivateAt.Value:dd/MM/yyyy HH:mm}."
                        : "Đã khóa tài khoản vô thời hạn.";
                    await ExecuteSuccessAsync(msg, UserActionType.Update, request.Id, TargetType.User);
                }
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Unban(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var result = await _userService.ToggleBanUserAsync(id);
            TempData[result ? "Success" : "Error"] = result
                ? "Đã mở khóa tài khoản thành công."
                : "Failed to update user status.";

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
                await ExecuteSuccessAsync($"Tạo tài khoản {model.RoleName} thành công.", UserActionType.AccountRegister, model.Email, TargetType.User);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }
    }
}
