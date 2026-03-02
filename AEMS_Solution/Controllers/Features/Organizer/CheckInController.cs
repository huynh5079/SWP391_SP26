using BusinessLogic.DTOs.Ticket;
using BusinessLogic.Service.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AEMS_Solution.Controllers.Features.Organizer
{
    [Authorize(Roles = "Organizer")]
    public class CheckInController : Controller
    {
        private readonly ICheckInService _checkInService;

        public CheckInController(ICheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        // GET: /CheckIn/Scanner?eventId=xxx
        public IActionResult Scanner(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                // Fallback, should navigate from an Event Detail page
                return RedirectToAction("Index", "Event");
            }

            ViewBag.EventId = eventId;
            return View();
        }

        // POST: /CheckIn/ProcessQR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessQR([FromBody] CheckInRequestDto request)
        {
            try
            {
                // Get current logged-in user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin đăng nhập." });
                }

                // Call service to process check-in
                var result = await _checkInService.ProcessCheckInAsync(request, userId);
                
                return Json(new 
                { 
                    success = result.IsSuccess, 
                    message = result.Message,
                    studentName = result.StudentName,
                    studentEmail = result.StudentEmail
                });
            }
            catch (Exception ex)
            {
                // Simple generic log; deeper logs handled inside service layers usually
                return Json(new { success = false, message = "Có lỗi hệ thống xảy ra: " + ex.Message });
            }
        }
    }
}
