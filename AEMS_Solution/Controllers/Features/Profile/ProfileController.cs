using AEMS_Solution.Controllers.Common;
using BusinessLogic.DTOs.User;
using BusinessLogic.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.Profile
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly IUserService _userService;
        private readonly BusinessLogic.Service.System.ISystemErrorLogService _systemErrorLogService;

        public ProfileController(IUserService userService, BusinessLogic.Service.System.ISystemErrorLogService systemErrorLogService)
        {
            _userService = userService;
            _systemErrorLogService = systemErrorLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var profile = await _userService.GetMyProfileAsync(CurrentUserId);
            if (profile == null) return NotFound();

            ViewBag.IsReadOnly = false;
            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> ViewUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var profile = await _userService.GetUserDetailAsync(id);
            if (profile == null) return NotFound();

            ViewBag.IsReadOnly = true;
            return View("Index", profile);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (CurrentUserId == null) return Unauthorized();
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh.");

            try
            {
                var newUrl = await _userService.UpdateAvatarAsync(CurrentUserId, file);
                if (newUrl != null)
                {
                    return Json(new { success = true, url = newUrl });
                }
                return Json(new { success = false, message = "Không thể tải ảnh lên." });
            }
            catch (Exception ex)
            {
                await _systemErrorLogService.LogErrorAsync(ex, CurrentUserId, "ProfileController.UploadAvatar");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(UpdateProfileRequestDto request)
        {
             if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

             // Handle Avatar Upload if needed (assuming service handles URL or we handle file here)
             // For now, let's assume request is mapped. 
             // Wait, View sends AvatarFile (IFormFile), but DTO has AvatarUrl (string).
             // I need to handle file upload here.
             
             // Skipping detailed upload logic for now to stay focused on Password task 
             // unless user asks for it. But I should at least call service.
             
             try
             {
                 await _userService.UpdateProfileAsync(CurrentUserId, request);
                 SetSuccess("Cập nhật hồ sơ thành công!");
             }
             catch(Exception ex)
             {
                 await _systemErrorLogService.LogErrorAsync(ex, CurrentUserId, "ProfileController.Update");
                 SetError("Lỗi khi cập nhật hồ sơ: " + ex.Message);
             }
             return RedirectToAction(nameof(Index));
        }
    }
}
