using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.Feedback.ForStudentFeedback;
using AutoMapper;
using BusinessLogic.DTOs.Student;
using BusinessLogic.Service.Student;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Event.Feedback
{
	[Authorize(Roles = "Student")]
	public class FeedbackController : BaseController
	{
		private readonly IStudentEventService _studentEventService;
		private readonly ISystemErrorLogService _errorLogService;
		private readonly IMapper _mapper;

		public FeedbackController(
			IStudentEventService studentEventService,
			ISystemErrorLogService errorLogService,
			IMapper mapper)
		{
			_studentEventService = studentEventService;
			_errorLogService = errorLogService;
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

				if (detail.EndTime >= DateTime.Now)
				{
					SetWarning("Chỉ có thể feedback sau khi sự kiện kết thúc.");
					return RedirectToAction("Detail", "StudentEvent", new { id = eventId });
				}

				var vm = _mapper.Map<StudentEventFeedbackViewModel>(detail);
				return View("~/Views/Event/Feedback/FeedbackForStudent/Index.cshtml", vm);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					ex,
					CurrentUserId,
					$"{nameof(FeedbackController)}.{nameof(Index)}");

				SetError(ex.Message);
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
				SetSuccess("Gửi feedback thành công. Cảm ơn bạn!");
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					ex,
					CurrentUserId,
					$"{nameof(FeedbackController)}.{nameof(Index)}_Post");

				SetError(ex.Message);
				return View("~/Views/Event/Feedback/FeedbackForStudent/Index.cshtml", vm);
			}

			return RedirectToAction("Detail", "StudentEvent", new { id = vm.EventId });
		}
	}
}
