using AEMS_Solution.Extensions;
using BusinessLogic.DTOs.User;
using BusinessLogic.Service.User;
using BusinessLogic.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Common
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;

        public ProfileController(IUserService userService, IFileStorageService fileStorageService)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var profile = await _userService.GetMyProfileAsync(userId);
            if (profile == null) return NotFound();

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromForm] UpdateProfileRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                // In a real app, reload the profile to show the view again with errors
                // For now, assuming basic validation
                TempData["Error"] = "Invalid data provided.";
                return RedirectToAction(nameof(Index)); // Or return View(formattedModel)
            }

            // Handle Avatar Upload Setup
            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                try
                {
                    // TODO: Implement actual upload logic when UploadContext is fully defined/confirmed
                    // Example: 
                    // var result = await _fileStorageService.UploadManyAsync(
                    //     new[] { request.AvatarFile }, 
                    //     UploadContext.UserAvatar, // Assuming context enum exists
                    //     userId);
                    // if (result.Any()) request.AvatarUrl = result.First().Url;
                    
                    // For now, ignoring upload as per request "Setup only"
                }
                catch (Exception ex)
                {
                    // Log error
                    TempData["Error"] = "Failed to upload avatar.";
                }
            }

            var success = await _userService.UpdateProfileAsync(userId, request);
            if (success)
            {
                TempData["Success"] = "Profile updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update profile.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
