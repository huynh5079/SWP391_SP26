using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.Feedback.ForStudentFeedback;
using AutoMapper;
using BusinessLogic.DTOs.Student;
using BusinessLogic.Service.Student;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;

namespace AEMS_Solution.Controllers.Event.Feedback
{
	[Authorize(Roles = "Student")]
	public class FeedbackController : BaseController
	{
		private readonly IStudentEventService _studentEventService;
		private readonly IMapper _mapper;

		public FeedbackController(
			IStudentEventService studentEventService,
			IMapper mapper)
		{
			_studentEventService = studentEventService;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string eventId)
		{
			if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

			try
			{
				var detail = await _studentEventService.GetEventDetailAsync(eventId, CurrentUserId);

				if (!detail.IsRegistered)
				{
					SetWarning("Bạn cần đăng ký sự kiện trước khi feedback.");
					return RedirectToAction("Detail", "StudentEvent", new { id = eventId });
				}

				if (detail.FeedbackStatus == DataAccess.Enum.FeedbackStatusEnum.BeforeEvent)
				{
					SetWarning("Chỉ được đánh giá sao trong hoặc sau khi sự kiện bắt đầu.");
					return RedirectToAction("Detail", "StudentEvent", new { id = eventId });
				}

				if (detail.HasSubmittedFeedback)
				{
					SetWarning("Bạn đã feedback sự kiện này rồi.");
					return RedirectToAction("Detail", "StudentEvent", new { id = eventId });
				}

				var vm = _mapper.Map<StudentEventFeedbackViewModel>(detail);
				return View("~/Views/Event/Feedback/FeedbackForStudent/Index.cshtml", vm);
			}
			catch (Exception ex)
			{
				await ExecuteErrorAsync(ex, ex.Message);
				return RedirectToAction("Index", "StudentEvent");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(StudentEventFeedbackViewModel vm)
		{
			if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

			if (!ModelState.IsValid)
			{
				return View("~/Views/Event/Feedback/FeedbackForStudent/Index.cshtml", vm);
			}

			try
			{
				var dto = _mapper.Map<SubmitFeedbackRequestDto>(vm);
				await _studentEventService.SubmitFeedbackAsync(CurrentUserId, vm.EventId, dto);
				await ExecuteSuccessAsync("Gửi feedback thành công. Cảm ơn bạn!", UserActionType.USER_SUBMITTED_FEEDBACK, vm.EventId, TargetType.Event);
			}
			catch (Exception ex)
			{
				await ExecuteErrorAsync(ex, ex.Message);
				return View("~/Views/Event/Feedback/FeedbackForStudent/Index.cshtml", vm);
			}

			return RedirectToAction("Detail", "StudentEvent", new { id = vm.EventId });
		}

		[HttpGet]
		public async Task<IActionResult> All(string eventId)
		{
			if (CurrentUserId == null) return RedirectToAction("Login", "Auth");
			if (string.IsNullOrWhiteSpace(eventId)) return RedirectToAction("Index", "StudentEvent");

			try
			{
				var detail = await _studentEventService.GetEventDetailAsync(eventId, CurrentUserId);
				var items = await _studentEventService.GetEventFeedbacksAsync(eventId);
				ViewBag.EventTitle = detail.Title;
				ViewBag.EventId = detail.EventId;
				return View("~/Views/Event/Feedback/FeedbackForStudent/All.cshtml", items);
			}
			catch (Exception ex)
			{
				await ExecuteErrorAsync(ex, ex.Message);
				return RedirectToAction("Detail", "StudentEvent", new { id = eventId });
			}
		}
		
	}
}
