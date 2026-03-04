using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using AEMS_Solution.Models.Organizer;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.ValiDate.ValidationDataforEvent;
using CloudinaryDotNet;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Linq;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;



[Authorize(Roles = "Organizer")] 
	public class OrganizerController : BaseController
	{
		private readonly IOrganizerService _organizerService;
		public OrganizerController(IOrganizerService organizerService)
		{
			_organizerService = organizerService;
		}
        [HttpGet]
    public async Task<IActionResult> Manage(string? operation, string? legacyAction, string? id, string? search = null, string? status = null, string? semesterId = null, string? location = null, string? department = null, string? timeState= null, DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1, int pageSize = 10)
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
						return await MyEvents(search, parsedStatus, semesterId,location, department, timeState, dateFrom, dateTo, page, pageSize);
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
			// Use service to get dropdowns (keeps controller free from direct DB queries)
			var dto = await _organizerService.GetCreateEventDropdownsAsync();

			vm.Semesters = dto.Semesters.Select(s => new SelectListItem { Value = s.Id, Text = s.Text }).ToList();
			vm.Departments = dto.Departments.Select(d => new SelectListItem { Value = d.Id, Text = d.Text }).ToList();
			vm.Locations = dto.Locations.Select(l => new SelectListItem { Value = l.Id, Text = l.Text }).ToList();
			vm.Topics = dto.Topics.Select(t => new SelectListItem { Value = t.Id, Text = t.Text }).ToList();
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

			if (!ModelState.IsValid)
			{
				await LoadDropdowns(vm);
				return View("~/Views/Event/CreateEvent.cshtml", vm);
			}

			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("Login", "Auth");

			// Map VM -> DTO
			var dto = new BusinessLogic.DTOs.Role.Organizer.CreateEventRequestDto
			{
				Title = vm.Title,
				Description = vm.Description,
				StartTime = vm.StartTime,
				EndTime = vm.EndTime,
				TopicId = vm.TopicId,
				LocationId = vm.LocationId,
				SemesterId = vm.SemesterId,
				DepartmentId = vm.DepartmentId,
				Capacity = vm.MaxCapacity,
				IsDepositRequired = vm.IsDepositRequired,
				DepositAmount = vm.DepositAmount,
				Mode = vm.Mode,
				Type = vm.Type,
				Status = vm.Status,
				BannerUrl = vm.ThumbnailUrl,
				MeetingUrl = vm.MeetingUrl,
				Agendas = vm.Agendas?.Select(a => new BusinessLogic.DTOs.Role.Organizer.CreateAgendaItemDto
				{
					SessionName = a.SessionName,
					Description = a.Description,
					SpeakerName = a.SpeakerName,
					StartTime = a.StartTime,
					EndTime = a.EndTime,
					Location = a.Location
				}).ToList() ?? new List<BusinessLogic.DTOs.Role.Organizer.CreateAgendaItemDto>()
				,
				// Provide reasonable default registration window derived from event start
				RegistrationOpenTime = vm.StartTime.AddDays(-7),
				RegistrationCloseTime = vm.StartTime.AddDays(-1)
			};

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
				var vm = new AEMS_Solution.Models.Event.EventDetailsViewModel
				{
					EventId = dto.EventId,
					Title = dto.Title,
					Description = dto.Description,
					ThumbnailUrl = dto.ThumbnailUrl,
					SemesterName = dto.SemesterName,
					DepartmentName = dto.DepartmentName,
					Location = dto.Location,
					StartTime = dto.StartTime,
					EndTime = dto.EndTime,
					MaxCapacity = dto.MaxCapacity,
					IsDepositRequired = dto.IsDepositRequired,
					DepositAmount = dto.DepositAmount,
					RegisteredCount = dto.RegisteredCount,
					CheckedInCount = dto.CheckedInCount,
					WaitlistCount = dto.WaitlistCount,
					AvgRating = dto.AvgRating,
					LastApprovalAction = dto.LastApprovalAction,
					LastApprovalComment = dto.LastApprovalComment,
					LastApprovalAt = dto.LastApprovalAt
				};

				foreach (var a in dto.Agendas)
				{
					vm.Agendas.Add(new AEMS_Solution.Models.Event.EventAgendaVm
					{
						Id = a.Id,
						EventId = a.EventId,
						SessionName = a.SessionName,
						Description = a.Description,
						SpeakerName = a.SpeakerName,
						StartTime = a.StartTime,
						EndTime = a.EndTime,
						Location = a.Location
					});
				}

			if (dto.Documents != null)
			{
				foreach (var d in dto.Documents)
				{
					vm.Documents.Add(new AEMS_Solution.Models.Event.EventDocumentVm
					{
						Id = d.Id,
						EventId = d.EventId,
						FileName = d.FileName ?? d.Url ?? "",
						Url = d.Url ?? "",
						Type = d.Type
					});
				}
			}

				vm.CanEdit = dto.CanEdit;
				vm.CanSendForApproval = dto.CanSendForApproval;

				return View("~/Views/Event/DetailEvent.cshtml", vm);
			}
			catch (InvalidOperationException)
			{
				return NotFound();
			}
		}
		// POST detail handler removed — use Manage POST if needed
		[HttpGet]
    public async Task<IActionResult> MyEvents(string? search, EventStatusEnum? status, string? semesterId, string? location, string? department, string? timeState, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 10)
    {
			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
        

        try
			{
            var paged = await _organizerService.GetMyEventsAsync(userId, search, status, semesterId, location, department, timeState, dateFrom, dateTo, page, pageSize);

            var vm = new MyEventsViewModel();
				var now = DateTimeHelper.GetVietnamTime();
				foreach (var e in paged.Items)
				{
					string displayStatus = e.Status.ToString();
					if (string.Equals(displayStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
						displayStatus = "Cancelled";
					else if (string.Equals(displayStatus, "Pending", StringComparison.OrdinalIgnoreCase))
						displayStatus = "Pending";
					else if (string.Equals(displayStatus, "Draft", StringComparison.OrdinalIgnoreCase))
						displayStatus = now > e.EndTime ? "Completed" : "Draft";
					else if (string.Equals(displayStatus, "Published", StringComparison.OrdinalIgnoreCase) || string.Equals(displayStatus, "Approved", StringComparison.OrdinalIgnoreCase))
						displayStatus = now < e.StartTime ? "Upcoming" : now >= e.StartTime && now <= e.EndTime ? "Happening" : "Completed";
					else
						displayStatus = now < e.StartTime ? "Upcoming" : now >= e.StartTime && now <= e.EndTime ? "Happening" : "Completed";

					vm.Events.Add(new OrganizerEventCardVm
					{
						EventId = e.EventId,
						Title = e.Title,
						ThumbnailUrl = e.ThumbnailUrl,
						SemesterId = e.SemesterId,
						SemesterName = e.SemesterName,
						DepartmentId = e.DepartmentId,
						DepartmentName = e.DepartmentName,
						Location = e.Location,
						StartTime = e.StartTime,
						EndTime = e.EndTime,
						MaxCapacity = e.MaxCapacity,
						Status = e.Status,
						RegisteredCount = e.RegisteredCount,
						CheckedInCount = e.CheckedInCount,
						WaitlistCount = e.WaitlistCount,
						AvgRating = e.AvgRating,
						Mode = e.Mode,
					    MeetingUrl = e.MeetingUrl,
					
					});
				}

				vm.Page = paged.Page;
				vm.PageSize = paged.PageSize;
				vm.TotalItems = paged.Total;
				vm.Search = search;
				vm.Status = status;
				vm.SemesterId = semesterId;
                vm.DateFrom = dateFrom;
                vm.DateTo = dateTo;
                vm.Location = location;
                vm.Department = department;
			    
			 
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

				var vm = new MyEventsViewModel();
				var now = DateTimeHelper.GetVietnamTime();
				foreach (var e in paged.Items)
				{
					string displayStatus = e.Status.ToString();
					if (string.Equals(displayStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
						displayStatus = "Cancelled";
					else if (string.Equals(displayStatus, "Pending", StringComparison.OrdinalIgnoreCase))
						displayStatus = "Pending";
					else if (string.Equals(displayStatus, "Draft", StringComparison.OrdinalIgnoreCase))
						displayStatus = now > e.EndTime ? "Completed" : "Draft";
					else if (string.Equals(displayStatus, "Published", StringComparison.OrdinalIgnoreCase) || string.Equals(displayStatus, "Approved", StringComparison.OrdinalIgnoreCase))
						displayStatus = now < e.StartTime ? "Upcoming" : now >= e.StartTime && now <= e.EndTime ? "Happening" : "Completed";
					else
						displayStatus = now < e.StartTime ? "Upcoming" : now >= e.StartTime && now <= e.EndTime ? "Happening" : "Completed";

					vm.Events.Add(new OrganizerEventCardVm
					{
						EventId = e.EventId,
						Title = e.Title,
						ThumbnailUrl = e.ThumbnailUrl,
						SemesterId = e.SemesterId,
						SemesterName = e.SemesterName,
						DepartmentId = e.DepartmentId,
						DepartmentName = e.DepartmentName,
						Location = e.Location,
						StartTime = e.StartTime,
						EndTime = e.EndTime,
						MaxCapacity = e.MaxCapacity,
						Status = e.Status,
						RegisteredCount = e.RegisteredCount,
						CheckedInCount = e.CheckedInCount,
						WaitlistCount = e.WaitlistCount,
						AvgRating = e.AvgRating,
						Mode = e.Mode,
						MeetingUrl = e.MeetingUrl,

					});
				}

				vm.Page = paged.Page;
				vm.PageSize = paged.PageSize;
				vm.TotalItems = paged.Total;
				vm.Search = search;
				vm.Status = status;
				vm.SemesterId = semesterId;

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

				var vm = new OrganizerDashboardViewModel();
				vm.Stats.TotalEvents = dto.TotalEvents;
				vm.Stats.UpcomingEvents = dto.UpcomingEvents;
				vm.Stats.DraftEvents = dto.DraftEvents;

                // Extensions for charts and cards
                vm.RegistrationsToday = dto.RegistrationsToday;
                vm.DepositCollectedThisMonth = dto.DepositCollectedThisMonth;
                vm.RegistrationTrendLabels = dto.RegistrationTrendLabels;
                vm.RegistrationTrendData = dto.RegistrationTrendData;
                vm.EventStatusDistribution = dto.EventStatusDistribution;
                
                if (dto.RecentFeedbacks != null && dto.RecentFeedbacks.Any())
                {
                    vm.RecentFeedbacks = dto.RecentFeedbacks.Select(f => new EventFeedbackSummaryVm
                    {
                        EventId = f.EventId,
                        EventTitle = f.EventTitle,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt,
                        StudentId = f.StudentId,
                        StudentCode = f.StudentCode
                    }).ToList();
                }

				vm.RecentEvents = dto.UpcomingList
					.Select(x => new OrganizerEventCardVm
					{
						EventId = x.Id,
						Title = x.Title,
						StartTime = x.StartTime,
					    Status = x.Status,
					})
					.ToList();

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

