using BusinessLogic.DTOs.Ticket;
using BusinessLogic.Service.Organizer.CheckIn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AEMS_Solution.Controllers.Features.Organizer
{
    [Authorize(Roles = "Organizer")]
    public class CheckInController : Controller
    {
        private readonly ICheckInService _checkInService;
        private readonly BusinessLogic.Service.Organizer.IOrganizerService _organizerService;

        public CheckInController(ICheckInService checkInService, BusinessLogic.Service.Organizer.IOrganizerService organizerService)
        {
            _checkInService = checkInService;
            _organizerService = organizerService;
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

        // POST: /CheckIn/ProcessCheckout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout([FromBody] CheckInRequestDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin đăng nhập." });
                }

                var result = await _checkInService.ProcessCheckoutAsync(request, userId);

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
                return Json(new { success = false, message = "Có lỗi hệ thống xảy ra: " + ex.Message });
            }
        }
        // GET: /CheckIn/LiveDisplay?eventId=xxx
        [AllowAnonymous]
        public async Task<IActionResult> LiveDisplay(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return RedirectToAction("Index", "Event");
            }

            try 
            {
                var eventDetail = await _organizerService.GetEventDetailsAsync(eventId);
                ViewBag.EventTitle = eventDetail.Title;
                ViewBag.ThumbnailUrl = eventDetail.ThumbnailUrl;
                ViewBag.ImageUrls = eventDetail.ImageUrls;
            }
            catch 
            {
                ViewBag.EventTitle = "AEMS System - Live Check-in";
            }
            
            ViewBag.EventId = eventId;
            return View();
        }
    }
}
