using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using AutoMapper;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.ValidationData.Event;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Event
{
	[Authorize(Roles = "Organizer")]
	public class EventController : BaseController
	{
		private readonly IOrganizerService _organizerService;
		private readonly IMapper _mapper;

		public EventController(IOrganizerService organizerService, IMapper mapper)
		{
			_organizerService = organizerService;
			_mapper = mapper;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendForApproval(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				SetError("Event id không hợp lệ.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			try
			{
				await _organizerService.SendForApprovalAsync(userId, id);
				SetSuccess("Gửi duyệt thành công.");
			}
			catch (InvalidOperationException ex)
			{
				SetError(ex.Message);
			}
			catch (EventValidator.BusinessValidationException ex)
			{
				SetError(ex.Message);
			}
			catch (Exception)
			{
				SetError("Đã xảy ra lỗi khi gửi duyệt. Vui lòng thử lại.");
			}

			return RedirectToAction("MyEvents", "Organizer");
		}

		private async Task LoadDropdowns(UpdateEventViewModel vm)
		{
			var dto = await _organizerService.GetCreateEventDropdownsAsync();
			_mapper.Map(dto, vm);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return RedirectToAction("Create", "Organizer");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(CreateEventViewModel vm)
		{
			return RedirectToAction("Create", "Organizer");
		}

		[HttpGet]
		[Route("Organizer/Edit/{id}")]
		public async Task<IActionResult> Edit(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return NotFound();
			}

			try
			{
				var dto = await _organizerService.GetEventDetailsAsync(id, CurrentUserId);
				var vm = _mapper.Map<UpdateEventViewModel>(dto);
				await LoadDropdowns(vm);
				return View("EditEvent", vm);
			}
			catch (InvalidOperationException)
			{
				return NotFound();
			}
		}

		[HttpPost]
		[Route("Organizer/Edit/{id}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(UpdateEventViewModel vm)
		{
			if (vm.EndTime <= vm.StartTime)
			{
				ModelState.AddModelError(nameof(vm.EndTime), "EndTime phải lớn hơn StartTime");
			}

			if (!vm.IsDepositRequired)
			{
				vm.DepositAmount = 0;
			}

			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			EventDetailsDto? originalEvent = null;
			try
			{
				originalEvent = await _organizerService.GetEventDetailsAsync(vm.EventId, userId);
			}
			catch (InvalidOperationException)
			{
				return NotFound();
			}

			var targetStatus = originalEvent.Status;
			if (originalEvent.Status == EventStatusEnum.Cancelled)
			{
				if (vm.Status == EventStatusEnum.Draft || vm.Status == EventStatusEnum.Pending)
				{
					targetStatus = vm.Status;
				}
				else
				{
					ModelState.AddModelError(nameof(vm.Status), "Chỉ được chuyển từ Cancelled sang Draft hoặc Pending.");
				}
			}

			if (!ModelState.IsValid)
			{
				await LoadDropdowns(vm);
				return View("EditEvent", vm);
			}

			var dto = _mapper.Map<UpdateEventRequestDto>(vm);
			dto.Status = targetStatus;

			try
			{
				await _organizerService.UpdateEventAsync(userId, vm.EventId, dto);
				SetSuccess(targetStatus == EventStatusEnum.Pending
					? "Cập nhật và chuyển sự kiện sang trạng thái chờ duyệt thành công."
					: "Cập nhật thành công!");
				return RedirectToAction("MyEvents", "Organizer");
			}
			catch (InvalidOperationException ex)
			{
				ModelState.AddModelError(string.Empty, ex.Message);
			}
			catch (EventValidator.BusinessValidationException ex)
			{
				ModelState.AddModelError(string.Empty, ex.Message);
			}

			await LoadDropdowns(vm);
			return View("EditEvent", vm);
		}

		
	}
}