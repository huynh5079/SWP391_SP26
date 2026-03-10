using System.Drawing.Printing;
using System.Linq;
using System.Net.NetworkInformation;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using AEMS_Solution.Models.Organizer;
using AEMS_Solution.Models.Organizer.Manage;
using AutoMapper;
using BusinessLogic.Service.Event.Sub_Service.Location;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.ValidationData.Event;
using CloudinaryDotNet;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
[Authorize(Roles = "Organizer")] 
	public class OrganizerController : BaseController
	{
	private readonly IOrganizerService _organizerService;
	    private readonly IMapper _mapper;
	    private readonly ILocationService _locationService;
	    private readonly IUnitOfWork _unitOfWork;
		public OrganizerController(IOrganizerService organizerService, IMapper mapper, ILocationService locationService, IUnitOfWork unitOfWork)
		{
			_organizerService = organizerService;
			_mapper = mapper;
			_locationService = locationService;
			_unitOfWork = unitOfWork;
		}
        [HttpGet]
        public async Task<IActionResult> Manage(string? operation, string? legacyAction, string? id, string? search = null, string? status = null, string? semesterId = null, int page = 1, int pageSize = 10)
        {
            // support both `operation` and legacy `action` param names
            var op = (operation ?? legacyAction)?.Trim();
            if (string.IsNullOrWhiteSpace(op)) return RedirectToAction("Index");

		try
		{
			switch (op.ToLowerInvariant())
			{
				case "create":
					// clear any model-state errors from binding of controller parameters
					ModelState.Clear();
					return await Create(); // GET create

				case "detailevent":
				case "detail":
					return await DetailEvent(id);

				//case "eventwaitlist":
				//return
				//case "update":
				case "myevents":
					{
						EventStatusEnum? parsedStatus = null;
						if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatusEnum>(status, true, out var tmp))
						{
							parsedStatus = tmp;
						}
						return await MyEvents(search, parsedStatus, semesterId, page, pageSize);
					}

				case "myeventsdelete":
					{
						EventStatusEnum? parsedStatus = null;
						if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatusEnum>(status, true, out var tmp))
						{
							parsedStatus = tmp;
						}
						return await MyEventsDelete(search, parsedStatus, semesterId, page, pageSize);
					}

				default:
					return BadRequest("Operation không hợp lệ.");
			}
		}
		catch (Exception ex)
		{
			// avoid exposing stack in production; set friendly error and redirect
			SetError("Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.");
			// optionally log ex (if you have logging), here we fallback to Index
			return RedirectToAction("Index");
		}
	}

	[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(string? operation, string? legacyAction, CreateEventViewModel vm, string? id)
        {
            var op = (operation ?? legacyAction)?.Trim();
            if (string.IsNullOrWhiteSpace(op)) return RedirectToAction("Index");

            try
            {
                switch (op.ToLowerInvariant())
                {
                    case "create":
                        return await Create(vm); // POST create

                    case "sendforapproval":
                    case "send":
                        return await SendForApproval(id);

                    case "publish":
                        return await PublishEvent(id);

                    case "cancel":
                        return await CancelEvent(id);
				    // case "eventwaitlist":
				    //return
				    //case "update":
                    case "softdelete":
                        if (string.IsNullOrEmpty(id))
                        {
                            SetError("Event id không hợp lệ.");
                            return RedirectToAction("MyEvents");
                        }

        

                        var userId = CurrentUserId;
                        if (string.IsNullOrEmpty(userId))
                        {
                            return RedirectToAction("Login", "Auth");
                        }

                        await _organizerService.SoftDeleteEventAsync(userId, id);
                        SetSuccess("Xóa sự kiện thành công.");
                        return RedirectToAction("MyEvents");

                    case "restore":
                        if (string.IsNullOrEmpty(id))
                        {
                            SetError("Event id không hợp lệ.");
                            return RedirectToAction("MyEventsDelete");
                        }

                        var restoreUser = CurrentUserId;
                        if (string.IsNullOrEmpty(restoreUser))
                        {
                            return RedirectToAction("Login", "Auth");
                        }

                        await _organizerService.RestoreEventAsync(restoreUser, id);
                        SetSuccess("Khôi phục sự kiện thành công.");
                        // quay lại danh sách đã xóa để người dùng thấy mục đã được gỡ khỏi đây
                        return RedirectToAction("MyEventsDelete");

                    case "detailevent":
                    case "detail":
                        return await DetailEvent(id);
                    default:
                        return BadRequest("Operation không hợp lệ.");
                }
            }
            catch (Exception ex)
            {
                SetError("Đã xảy ra lỗi khi xử lý yêu cầu POST. Vui lòng thử lại.");
                return RedirectToAction("Index");
            }
        }
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> PublishEvent(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			SetError("Event id không hợp lệ.");
			return RedirectToAction("MyEvents", "Organizer");
		}

		var userId = CurrentUserId;
		if (string.IsNullOrEmpty(userId))
			return RedirectToAction("Login", "Auth");

		try
		{
			await _organizerService.PublishEventAsync(userId, id);
			SetSuccess("Public event thành công.");
		}
		catch (InvalidOperationException ex)
		{
			SetError(ex.Message);
		}
		catch (Exception)
		{
			SetError("Đã xảy ra lỗi khi public event. Vui lòng thử lại.");
		}

		return RedirectToAction("MyEvents", "Organizer");
	}



	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CancelEvent(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			SetError("Event id không hợp lệ.");
			return RedirectToAction("MyEvents", "Organizer");
		}

		var userId = CurrentUserId;
		if (string.IsNullOrEmpty(userId))
			return RedirectToAction("Login", "Auth");

		try
		{
			await _organizerService.CancelEventAsync(userId, id);
			SetSuccess("Hủy sự kiện thành công.");
		}
		catch (InvalidOperationException ex)
		{
			SetError(ex.Message);
		}
		catch (Exception)
		{
			SetError("Đã xảy ra lỗi khi hủy sự kiện. Vui lòng thử lại.");
		}

		return RedirectToAction("MyEvents", "Organizer");
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
				return RedirectToAction("Login", "Auth");

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
		//load drop down for event
		//load drop down for event
		private async Task LoadDropdowns(CreateEventViewModel vm)
		{
			var dto = await _organizerService.GetCreateEventDropdownsAsync();
			_mapper.Map(dto, vm);
		}

		[HttpGet]
		public async Task<IActionResult> GetAvailableLocations(DateTime startTime, DateTime endTime)
		{
			if (endTime <= startTime)
			{
				return Json(new List<object>());
			}

			var locations = await _locationService.GetAvailableLocationsAsync(startTime, endTime);
			return Json(locations.Select(x => new
			{
				id = x.LocationId,
				name = x.Name,
				address = x.Address,
				building = ExtractAddressPart(x.Address, "Building"),
				floor = ExtractAddressPart(x.Address, "Floor"),
				room = ExtractAddressPart(x.Address, "Room"),
				capacity = x.Capacity,
				type = x.Type?.ToString()
			}));
		}

		private static string ExtractAddressPart(string? address, string label)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				return string.Empty;
			}

			var segment = address
				.Split(" - ", StringSplitOptions.RemoveEmptyEntries)
				.FirstOrDefault(x => x.StartsWith(label + " ", StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(segment))
			{
				return string.Empty;
			}

			return segment.Substring(label.Length).Trim();
		}

		
		// Event/Create
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var vm = new CreateEventViewModel
			{
				Agendas = new List<CreateAgendaItemVm> { new CreateAgendaItemVm() }
			};

			// remove any residual modelstate keys from route/model binding (e.g. id/operation)
			ModelState.Remove("id");
			ModelState.Remove("operation");
			ModelState.Remove("legacyAction");

			await LoadDropdowns(vm);
			return View("~/Views/Event/CreateEvent.cshtml", vm);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CreateEventViewModel vm)
		{
			if (vm.EndTime <= vm.StartTime)
				ModelState.AddModelError(nameof(vm.EndTime), "EndTime phải lớn hơn StartTime");

			if (!vm.IsDepositRequired)
				vm.DepositAmount = 0;

			if (string.IsNullOrWhiteSpace(vm.LocationId))
			{
				ModelState.AddModelError(nameof(vm.LocationId), "Vui lòng chọn phòng hợp lệ.");
			}
			else
			{
				var selectedLocation = await _locationService.GetLocationByIdAsync(vm.LocationId);
				if (selectedLocation == null)
				{
					ModelState.AddModelError(nameof(vm.LocationId), "Phòng đã chọn không tồn tại.");
				}
				else
				{
					if (vm.MaxCapacity > selectedLocation.Capacity)
					{
						ModelState.AddModelError(nameof(vm.MaxCapacity), $"Sức chứa sự kiện phải nhỏ hơn hoặc bằng sức chứa phòng ({selectedLocation.Capacity}).");
					}

					if (vm.EndTime > vm.StartTime)
					{
						var availableLocations = await _locationService.GetAvailableLocationsAsync(vm.StartTime, vm.EndTime);
						if (!availableLocations.Any(x => x.LocationId == vm.LocationId))
						{
							ModelState.AddModelError(nameof(vm.LocationId), "Phòng đã chọn không còn khả dụng trong khoảng thời gian này.");
						}
					}
				}
			}

			if (!ModelState.IsValid)
			{
				await LoadDropdowns(vm);
				return View("~/Views/Event/CreateEvent.cshtml", vm);
			}

			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("Login", "Auth");
			
			// Map VM -> DTO via AutoMapper
			var dto = _mapper.Map<BusinessLogic.DTOs.Role.Organizer.CreateEventRequestDto>(vm);
			// Provide reasonable default registration window derived from event start
			dto.RegistrationOpenTime = vm.StartTime.AddDays(-7);
			dto.RegistrationCloseTime = vm.StartTime.AddDays(-1);

			try
			{
				//Console.WriteLine($"VM Mode={vm.Mode}, MeetingUrl={vm.MeetingUrl}");
				//Console.WriteLine($"DTO Mode={dto.Mode}, MeetingUrl={dto.MeetingUrl}");
				await _organizerService.CreateEventAsync(userId, dto);
				SetSuccess("Tạo Event thành công (Draft).");
				return RedirectToAction("Index", "Organizer");
			}
			catch (InvalidOperationException ex)
			{
				ModelState.AddModelError("", ex.Message);
				await LoadDropdowns(vm);
				return View("~/Views/Event/CreateEvent.cshtml", vm);
			}
			catch (EventValidator.BusinessValidationException ex)
			{
				// Validation from validator -> show on page
				ModelState.AddModelError("", ex.Message);
				await LoadDropdowns(vm);
				return View("~/Views/Event/CreateEvent.cshtml", vm);
			}
		}
		//end create event


		//view detail aboout event
		[HttpGet]
		public async Task<IActionResult> DetailEvent(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			try
			{
				var dto = await _organizerService.GetEventDetailsAsync(id, CurrentUserId);
				var vm = _mapper.Map<AEMS_Solution.Models.Event.EventDetailsViewModel>(dto);

				return View("~/Views/Event/DetailEvent.cshtml", vm);
			}
			catch (InvalidOperationException)
			{
				return NotFound();
			}
		}
		// POST detail handler removed — use Manage POST if needed
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateAgendaFromDetail(CreateDetailAgendaViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.EventId))
			{
				SetError("Event id không hợp lệ.");
				return RedirectToAction(nameof(MyEvents));
			}

			if (string.IsNullOrWhiteSpace(CurrentUserId))
			{
				return RedirectToAction("Login", "Auth");
			}

			if (!ModelState.IsValid)
			{
				SetError(ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu agenda không hợp lệ.");
				return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
			}

			var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == model.EventId && x.OrganizerId == CurrentUserId && x.DeletedAt == null);
			if (eventEntity == null)
			{
				SetError("Không tìm thấy sự kiện để thêm agenda.");
				return RedirectToAction(nameof(MyEvents));
			}

			if (model.StartTime >= model.EndTime)
			{
				SetError("Thời gian kết thúc agenda phải lớn hơn thời gian bắt đầu.");
				return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
			}

			if (model.StartTime < eventEntity.StartTime || model.EndTime > eventEntity.EndTime)
			{
				SetError("Thời gian agenda phải nằm trong thời gian của event.");
				return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
			}

			var agenda = new DataAccess.Entities.EventAgenda
			{
				EventId = model.EventId,
				SessionName = model.SessionName.Trim(),
				SpeakerName = model.SpeakerName.Trim(),
				Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
				StartTime = model.StartTime,
				EndTime = model.EndTime,
				Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim(),
				UpdatedAt = DateTimeHelper.GetVietnamTime()
			};

			await _unitOfWork.EventAgenda.CreateAsync(agenda);
			await _unitOfWork.SaveChangesAsync();

			SetSuccess("Tạo agenda thành công.");
			return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateDocumentFromDetail(CreateDetailDocumentViewModel model)
		{
			if (string.IsNullOrWhiteSpace(model.EventId))
			{
				SetError("Event id không hợp lệ.");
				return RedirectToAction(nameof(MyEvents));
			}

			if (string.IsNullOrWhiteSpace(CurrentUserId))
			{
				return RedirectToAction("Login", "Auth");
			}

			if (!ModelState.IsValid)
			{
				SetError(ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu tài liệu không hợp lệ.");
				return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
			}

			var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == model.EventId && x.OrganizerId == CurrentUserId && x.DeletedAt == null);
			if (eventEntity == null)
			{
				SetError("Không tìm thấy sự kiện để thêm tài liệu.");
				return RedirectToAction(nameof(MyEvents));
			}

			var document = new DataAccess.Entities.EventDocument
			{
				EventId = model.EventId,
				Name = model.Name.Trim(),
				Url = model.Url.Trim(),
				Type = string.IsNullOrWhiteSpace(model.Type) ? null : model.Type.Trim(),
				UpdatedAt = DateTimeHelper.GetVietnamTime()
			};

			await _unitOfWork.EventDocuments.CreateAsync(document);
			await _unitOfWork.SaveChangesAsync();

			SetSuccess("Tạo document thành công.");
			return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
		}

		[HttpGet]
		public async Task<IActionResult> MyEvents(string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
		{
			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

			try
			{
				var paged = await _organizerService.GetMyEventsAsync(userId, search, status, semesterId, page, pageSize);

				var vm = new MyEventsViewModel
				{
					Events = _mapper.Map<List<OrganizerEventCardVm>>(paged.Items),
					Page = paged.Page,
					PageSize = paged.PageSize,
					TotalItems = paged.Total,
					Search = search,
					Status = status,
					SemesterId = semesterId
				};

				return View("~/Views/Event/MyEvent.cshtml", vm);
			}
			catch (InvalidOperationException ex)
			{
				SetError($"Lỗi: {ex.Message}");
				return View("~/Views/Event/MyEvent.cshtml", new MyEventsViewModel());
			}
			catch (Exception ex)
			{
				SetError("Đã xảy ra lỗi khi tải danh sách. Vui lòng thử lại hoặc liên hệ quản trị viên.");
				return View("~/Views/Event/MyEvent.cshtml", new MyEventsViewModel());
			}
		}
	        public async Task<IActionResult> MyEventsDelete(string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
	        {
			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

			try
			{
				var paged = await _organizerService.GetMyDeletedEventsAsync(userId, search, status, semesterId, page, pageSize);

				var vm = new MyEventsViewModel
				{
					Events = _mapper.Map<List<OrganizerEventCardVm>>(paged.Items),
					Page = paged.Page,
					PageSize = paged.PageSize,
					TotalItems = paged.Total,
					Search = search,
					Status = status,
					SemesterId = semesterId
				};

				return View("~/Views/Event/MyEventDelete.cshtml", vm);
			}
			catch (InvalidOperationException ex)
			{
				SetError($"Lỗi: {ex.Message}");
				return View("~/Views/Event/MyEventDelete.cshtml", new MyEventsViewModel());
			}
			catch (Exception)
			{
				SetError("Đã xảy ra lỗi khi tải danh sách. Vui lòng thử lại hoặc liên hệ quản trị viên.");
				return View("~/Views/Event/MyEventDelete.cshtml", new MyEventsViewModel());
			}
		}
	
	//default
	public async Task<IActionResult> Index()
		{
			try
			{
				var userId = CurrentUserId;
				
				if (string.IsNullOrEmpty(userId))
				{
					return RedirectToAction("Login", "Auth");
				}
				
				var dto = await _organizerService.GetDashboardAsync(userId);

				var vm = _mapper.Map<OrganizerDashboardViewModel>(dto);

				return View(vm);
			}
			catch (InvalidOperationException ex)
			{
				SetError($"Lỗi: {ex.Message}");
				return View(new OrganizerDashboardViewModel());
			}
			catch (Exception ex)
			{
				SetError("Đã xảy ra lỗi. Vui lòng liên hệ quản trị viên.");
				return View(new OrganizerDashboardViewModel());
			}
		}
	}

