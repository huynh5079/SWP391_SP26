using System.Linq.Expressions;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using AEMS_Solution.Models.Organizer;
using BusinessLogic.Service.Organizer;
using DataAccess.Entities;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DataAccess.Helper;

namespace AEMS_Solution.Controllers.Event
{

	[Authorize(Roles = "Organizer")]
	public class EventController : BaseController
	{
		private readonly AEMSContext _db;
		private readonly IOrganizerService _organizerService;
		public EventController(AEMSContext db, IOrganizerService organizerService)
		{
			_db = db;
			_organizerService = organizerService;
		}
		//gửi duyệt

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

			var staff = await _db.StaffProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
			if (staff == null)
			{
				SetError("Organizer chưa có StaffProfile.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
			if (ev == null)
			{
				SetError("Event không tồn tại.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			if (ev.OrganizerId != staff.Id)
			{
				SetError("Bạn không có quyền thực hiện hành động này.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			if (ev.Status == EventStatusEnum.Pending)
			{
				SetError("Event đã ở trạng thái Pending.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			// Only Draft can be sent for approval
			if (ev.Status != EventStatusEnum.Draft)
			{
				SetError("Chỉ event ở trạng thái Draft mới có thể gửi duyệt.");
				return RedirectToAction("MyEvents", "Organizer");
			}

			ev.Status = EventStatusEnum.Pending;
			ev.UpdatedAt = DateTimeHelper.GetVietnamTime();
			await _db.SaveChangesAsync();

			SetSuccess("Gửi duyệt thành công.");
			return RedirectToAction("MyEvents", "Organizer");
		}

		private async Task LoadDropdowns(CreateEventViewModel vm)
		{
			vm.Semesters = await _db.Semesters.AsNoTracking()
				.OrderByDescending(x => x.StartDate)
				.Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
				.ToListAsync();

			vm.Departments = await _db.Departments.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
				.ToListAsync();

			vm.Locations = await _db.Locations.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
				.ToListAsync();

			vm.Topics = await _db.Topics.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
				.ToListAsync();
		}
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var vm = new CreateEventViewModel
			{
				Agendas = new List<CreateAgendaItemVm> { new CreateAgendaItemVm() }
			};

			await LoadDropdowns(vm);
			return View("CreateEvent", vm);
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
				return View("CreateEvent", vm);
			}

			var userId = CurrentUserId;
			if (string.IsNullOrEmpty(userId))
				return RedirectToAction("Login", "Auth");

			// FK OrganizerId -> StaffProfile.Id
			var staff = await _db.StaffProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
			if (staff == null)
			{
				ModelState.AddModelError("", "Organizer chưa có StaffProfile.");
				await LoadDropdowns(vm);
				return View("CreateEvent", vm);
			}

			// Optional: chặn FK crash (recommended)
			if (!await _db.Semesters.AnyAsync(x => x.Id == vm.SemesterId))
			{
				ModelState.AddModelError(nameof(vm.SemesterId), "Semester không tồn tại.");
				await LoadDropdowns(vm);
				return View("CreateEvent", vm);
			}

			if (!await _db.Locations.AnyAsync(x => x.Id == vm.LocationId))
			{
				ModelState.AddModelError(nameof(vm.LocationId), "Location không tồn tại.");
				await LoadDropdowns(vm);
				return View("CreateEvent", vm);
			}

			if (!string.IsNullOrEmpty(vm.DepartmentId) &&
					!await _db.Departments.AnyAsync(x => x.Id == vm.DepartmentId))
			{
				ModelState.AddModelError(nameof(vm.DepartmentId), "Department không tồn tại.");
				await LoadDropdowns(vm);
				return View("CreateEvent", vm);
			}

			if (!string.IsNullOrEmpty(vm.TopicId) &&
					!await _db.Topics.AnyAsync(x => x.Id == vm.TopicId))
			{
				ModelState.AddModelError(nameof(vm.TopicId), "Topic không tồn tại.");
				await LoadDropdowns(vm);
				return View("CreateEvent", vm);
			}

			var now = DateTime.Now; // nếu bạn có VietnamTime helper thì thay
			var eventId = Guid.NewGuid().ToString();

			// ✅ chỗ quan trọng: gán LocationId, không gán Location object bằng string
			var e = new DataAccess.Entities.Event
			{
				Id = eventId,
				Title = vm.Title.Trim(),
				Description = vm.Description,
				ThumbnailUrl = vm.ThumbnailUrl,

				OrganizerId = staff.Id,
				SemesterId = vm.SemesterId,
				DepartmentId = vm.DepartmentId,

				LocationId = vm.LocationId,
				TopicId = vm.TopicId,

				StartTime = vm.StartTime,
				EndTime = vm.EndTime,
				MaxCapacity = vm.MaxCapacity,

				IsDepositRequired = vm.IsDepositRequired,
				DepositAmount = vm.IsDepositRequired ? vm.DepositAmount : 0,

				Type = vm.Type,
				Status = vm.Status,

				CreatedAt = now,
				UpdatedAt = now,
				PublishedAt = null
			};

			_db.Events.Add(e);

			// Insert agendas (0..n)
			if (vm.Agendas != null && vm.Agendas.Count > 0)
			{
				for (int i = 0; i < vm.Agendas.Count; i++)
				{
					var a = vm.Agendas[i];

					bool isEmpty =
						string.IsNullOrWhiteSpace(a.SessionName) &&
						string.IsNullOrWhiteSpace(a.SpeakerName) &&
						string.IsNullOrWhiteSpace(a.Description) &&
						a.StartTime == null && a.EndTime == null &&
						string.IsNullOrWhiteSpace(a.Location);

					if (isEmpty) continue;

					// ✅ Validate StartTime và EndTime
					if (a.StartTime != null && a.EndTime != null)
					{
						if (a.EndTime <= a.StartTime)
						{
							ModelState.AddModelError($"Agendas[{i}].EndTime", $"Agenda #{i + 1}: End time phải lớn hơn Start time");
							await LoadDropdowns(vm);
							return View("CreateEvent", vm);
						}

						// ✅ Kiểm tra agenda time có nằm trong event time không (optional)
						if (a.StartTime < vm.StartTime || a.EndTime > vm.EndTime)
						{
							ModelState.AddModelError($"Agendas[{i}].StartTime", $"Agenda #{i + 1}: Thời gian agenda phải nằm trong thời gian event");
							await LoadDropdowns(vm);
							return View("CreateEvent", vm);
						}
					}
					else if ((a.StartTime != null && a.EndTime == null) || (a.StartTime == null && a.EndTime != null))
					{
						// ✅ Nếu có một trong hai time thì cần cả hai
						ModelState.AddModelError($"Agendas[{i}].StartTime", $"Agenda #{i + 1}: Cần nhập cả Start time và End time");
						await LoadDropdowns(vm);
						return View("CreateEvent", vm);
					}

					_db.EventAgenda.Add(new EventAgenda
					{
						Id = Guid.NewGuid().ToString(),
						EventId = eventId,
						SessionName = a.SessionName,
						Description = a.Description,
						SpeakerName = a.SpeakerName,
						StartTime = a.StartTime,
						EndTime = a.EndTime,
						Location = a.Location,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			// Insert documents (if any)
			if (vm.Documents != null && vm.Documents.Count > 0)
			{
				foreach (var d in vm.Documents)
				{
					if (string.IsNullOrWhiteSpace(d.Url) && string.IsNullOrWhiteSpace(d.FileName)) continue;

					_db.EventDocuments.Add(new EventDocument
					{
						Id = Guid.NewGuid().ToString(),
						EventId = eventId,
						Name = d.FileName,
						Url = d.Url,
						Type = d.Type,
						CreatedAt = now,
						UpdatedAt = now
					});
				}
			}

			await _db.SaveChangesAsync();

			SetSuccess("Tạo Event thành công (Draft).");
			return RedirectToAction("Index", "Organizer");
		}
		// Edit event
        private async Task LoadDropdowns(UpdateEventViewModel vm)
        {
            vm.Semesters = await _db.Semesters.AsNoTracking()
                .OrderByDescending(x => x.StartDate)
                .Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
                .ToListAsync();

            vm.Departments = await _db.Departments.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
                .ToListAsync();

            vm.Locations = await _db.Locations.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
                .ToListAsync();

            vm.Topics = await _db.Topics.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id, Text = x.Name })
                .ToListAsync();
        }

        [HttpGet]
        [Route("Organizer/Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // 1. Kiểm tra lại chính xác tên thuộc tính EventAgendas hay EventAgenda
            var ev = await _db.Events
                .Include(e => e.EventAgenda) // Đảm bảo tên này khớp với class Event.cs
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            var vm = new UpdateEventViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                SemesterId = ev.SemesterId,
                DepartmentId = ev.DepartmentId,
                LocationId = ev.LocationId,
                TopicId = ev.TopicId,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                MaxCapacity = ev.MaxCapacity,
                ThumbnailUrl = ev.ThumbnailUrl,
                Type = (EventTypeEnum)ev.Type,
                Status = (EventStatusEnum)ev.Status,
                IsDepositRequired = (bool)ev.IsDepositRequired,
                DepositAmount = (decimal)ev.DepositAmount,
                Mode = ev.Mode ?? EventModeEnum.Offline,
                MeetingUrl = ev.MeetingUrl,
                // 2. Thêm .ToList() ở cuối để sửa lỗi kiểu dữ liệu
                Agendas = ev.EventAgenda.Select(a => new UpdateAgendaItemVm
                {
                    Id = a.Id,
                    SessionName = a.SessionName,
                    Description = a.Description,
                    SpeakerName = a.SpeakerName,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Location = a.Location // Đừng quên gán cả Location cho Agenda
                }).ToList()
            };

            await LoadDropdowns(vm);
            return View("EditEvent", vm);
        }
        [HttpPost]
        [Route("Organizer/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateEventViewModel vm)
        {
            if (vm.EndTime <= vm.StartTime)
                ModelState.AddModelError(nameof(vm.EndTime), "EndTime phải lớn hơn StartTime");

            if (!vm.IsDepositRequired)
                vm.DepositAmount = 0;

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm);
                return View("EditEvent", vm);
            }

            // Lấy dữ liệu gốc kèm theo Agenda hiện tại
            var ev = await _db.Events.Include(e => e.EventAgenda).FirstOrDefaultAsync(e => e.Id == vm.EventId);
            if (ev == null) return NotFound();

            var originalStatus = ev.Status;

            // Cho phép đổi trạng thái chỉ khi event đang Cancelled và chỉ được chuyển sang Draft hoặc Pending
            var targetStatus = originalStatus;
            if (originalStatus == EventStatusEnum.Cancelled)
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

            // 1. Cập nhật thông tin cơ bản
            ev.Title = vm.Title;
            ev.Description = vm.Description;
            ev.SemesterId = vm.SemesterId;
            ev.LocationId = vm.LocationId;
            ev.DepartmentId = vm.DepartmentId;
            ev.TopicId = vm.TopicId;
            ev.StartTime = vm.StartTime;
            ev.EndTime = vm.EndTime;
            ev.MaxCapacity = vm.MaxCapacity;
            ev.ThumbnailUrl = vm.ThumbnailUrl;
            ev.Type = vm.Type;
            ev.Status = targetStatus;
            ev.IsDepositRequired = vm.IsDepositRequired;
            ev.DepositAmount = vm.DepositAmount;
            ev.Mode = vm.Mode;
            ev.MeetingUrl = vm.MeetingUrl;
            ev.UpdatedAt = DateTime.Now;

            // 2. Cập nhật Agendas (Cách đơn giản nhất: Xóa cũ, thêm mới)
            _db.EventAgenda.RemoveRange(ev.EventAgenda);
            // Remove existing documents as well (simplest approach)
            var existingDocs = await _db.EventDocuments.Where(ed => ed.EventId == ev.Id).ToListAsync();
            if (existingDocs.Any()) _db.EventDocuments.RemoveRange(existingDocs);
            if (vm.Agendas != null)
            {
                foreach (var item in vm.Agendas)
                {
                    if (string.IsNullOrEmpty(item.SessionName)) continue;
                    _db.EventAgenda.Add(new EventAgenda
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = ev.Id,
                        SessionName = item.SessionName,
                        SpeakerName = item.SpeakerName,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Location = item.Location
                    });
                }
            }

            // Add updated documents
            if (vm.Documents != null)
            {
                foreach (var d in vm.Documents)
                {
                    if (string.IsNullOrWhiteSpace(d.Url) && string.IsNullOrWhiteSpace(d.FileName)) continue;

                    _db.EventDocuments.Add(new EventDocument
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = ev.Id,
                        Name = d.FileName,
                        Url = d.Url,
                        Type = d.Type,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();

            // Nếu từ Cancelled chuyển sang Pending -> gửi duyệt ngay
            if (originalStatus == EventStatusEnum.Cancelled && targetStatus == EventStatusEnum.Pending)
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }
                await _organizerService.SendForApprovalAsync(userId, ev.Id);
                SetSuccess("Cập nhật và gửi duyệt thành công.");
            }
            else
            {
                SetSuccess("Cập nhật thành công!");
            }
            return RedirectToAction("MyEvents", "Organizer");
        }
        //view detail aboout event
        [HttpGet]
		public void DetailEvent()
		{

		}
        

		
	}
}