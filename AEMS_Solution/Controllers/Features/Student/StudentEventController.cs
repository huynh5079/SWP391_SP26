using AEMS_Solution.Controllers.Common;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.DTOs.Student;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Student;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.Student
{
    [Authorize(Roles = "Student")]
    public class StudentEventController : BaseController
    {
        private readonly IStudentEventService _service;
        private readonly ISystemErrorLogService _errorLog;
        private readonly IEventWaitlistService _waitlistService;

        public StudentEventController(IStudentEventService service, ISystemErrorLogService errorLog, IEventWaitlistService waitlistService)
        {
            _service = service;
            _errorLog = errorLog;
            _waitlistService = waitlistService;
        }

        // ─── Browse (weekly calendar) ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? topicId,
            string? semesterId,
            int weekOffset = 0)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var events = await _service.GetPublishedEventsAsync(
                CurrentUserId, search, topicId, semesterId);

            var stats = await _service.GetDashboardStatsAsync(CurrentUserId);

            ViewBag.WeekOffset = weekOffset;
            ViewBag.Search = search;

            var vm = new StudentEventBrowseViewModel
            {
                Stats = stats,
                Events = events
            };

            return View(vm);
        }

        // ─── Event detail ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var detail = await _service.GetEventDetailAsync(id, CurrentUserId);
            ViewBag.AllFeedbacks = await _service.GetEventFeedbacksAsync(id);
            return View(detail);
        }

        // ─── Register ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string id)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.RegisterForEventAsync(CurrentUserId, id);
                SetSuccess("Đăng ký thành công!");
            }
            catch (Exception ex)
            {
                // Log full exception (incl. inner) to system error log
                await _errorLog.LogErrorAsync(
                    ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(Register)}");

                // Show deepest message to user
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─── Cancel registration ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string ticketId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.CancelRegistrationAsync(CurrentUserId, ticketId);
                SetSuccess("Hủy đăng ký thành công.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(
                    ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(Cancel)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id = eventId });
        }

        // ─── My participations ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var events = await _service.GetMyParticipationsAsync(CurrentUserId);
            var waitlist = await _waitlistService.GetMyWaitlistAsync(CurrentUserId); // ← thêm
            ViewBag.Waitlist = waitlist;
            return View("~/Views/StudentEvent/MyEvents.cshtml", events);
        }

        // ─── Submit feedback ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(string id, SubmitFeedbackRequestDto dto)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu feedback không hợp lệ.");
                return RedirectToAction(nameof(Detail), new { id });
            }

            try
            {
                await _service.SubmitFeedbackAsync(CurrentUserId, id, dto);
                SetSuccess("Feedback đã được gửi. Cảm ơn bạn!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(
                    ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(SubmitFeedback)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }
        // ─── Join Waitlist ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitlist(string id)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.AddToWaitlistAsync(CurrentUserId, id);
                SetSuccess("Đã đăng ký vào danh sách chờ. Bạn sẽ được thông báo khi có chỗ trống.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(
                    ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(JoinWaitlist)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─── Accept Waitlist Offer ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptWaitlist(string id)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var detail = await _service.GetEventDetailAsync(id, CurrentUserId);
                await _waitlistService.RespondToOfferAsync(new RespondOfferRequestDto
                {
                    EventId = id,
                    StudentId = detail.WaitlistStudentProfileId!,
                    Accept = true
                });
                SetSuccess("Đã xác nhận tham gia! Vé của bạn đã được tạo.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(AcceptWaitlist)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─── Decline Waitlist Offer ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineWaitlist(string id)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var detail = await _service.GetEventDetailAsync(id, CurrentUserId);
                await _waitlistService.RespondToOfferAsync(new RespondOfferRequestDto
                {
                    EventId = id,
                    StudentId = detail.WaitlistStudentProfileId!,
                    Accept = false
                });
                SetSuccess("Đã từ chối. Chỗ trống sẽ được nhường cho người tiếp theo.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(StudentEventController)}.{nameof(DeclineWaitlist)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
