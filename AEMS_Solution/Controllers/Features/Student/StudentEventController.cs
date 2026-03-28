using AEMS_Solution.Controllers.Common;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.DTOs.Student;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Student;
using BusinessLogic.Service.System;
using BusinessLogic.Service.UserActivities;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Features.Student
{
    [Authorize(Roles = "Student")]
    public class StudentEventController : BaseController
    {
        private readonly IStudentEventService _service;
        private readonly IEventWaitlistService _waitlistService;

        public StudentEventController(IStudentEventService service, IEventWaitlistService waitlistService)
        {
            _service = service;
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

            // Log activity
            await LogUserActivity(UserActionType.USER_VIEWED_EVENT, id, TargetType.Event, detail?.Title);

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
                await ExecuteSuccessAsync("Đăng ký thành công!", UserActionType.USER_REGISTERED_EVENT, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
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
                await ExecuteSuccessAsync("Hủy đăng ký thành công.", UserActionType.USER_CANCELLED_EVENT, eventId, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
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
                await ExecuteSuccessAsync("Feedback đã được gửi. Cảm ơn bạn!", UserActionType.USER_SUBMITTED_FEEDBACK, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
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
                await ExecuteSuccessAsync("Đã đăng ký vào danh sách chờ. Bạn sẽ được thông báo khi có chỗ trống.", UserActionType.USER_JOINED_WAITLIST, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
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
                await ExecuteSuccessAsync("Đã xác nhận tham gia! Vé của bạn đã được tạo.", UserActionType.USER_RESPONDED_OFFER, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
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
                await ExecuteSuccessAsync("Đã từ chối. Chỗ trống sẽ được nhường cho người tiếp theo.", UserActionType.USER_RESPONDED_OFFER, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
            }

            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
