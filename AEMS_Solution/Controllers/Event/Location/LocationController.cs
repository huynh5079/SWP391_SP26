using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Location;
using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.Service.Event.Sub_Service.Location;
using BusinessLogic.Service.ValidationData.Loction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;

namespace AEMS_Solution.Controllers.Event.Location
{
    [Authorize(Roles = "Approver,Admin,Organizer")]
    public class LocationController : BaseController
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? filterStatus, int page = 1)
        {
            var locations = await _locationService.GetAllLocationsAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                locations = locations
                    .Where(x => x.Address.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(filterStatus) &&
                System.Enum.TryParse<DataAccess.Enum.LocationStatusEnum>(filterStatus, out var parsedStatus))
            {
                locations = locations.Where(x => x.Status == parsedStatus).ToList();
            }

            int pageSize = 10;
            int totalRecords = locations.Count;
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (totalPages == 0) totalPages = 1;

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            locations = locations.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new LocationIndexViewModel
            {
                Search = search,
                FilterStatus = filterStatus,
                PageNumber = page,
                TotalPages = totalPages,
                Locations = locations.Select(x => new LocationListItemVm
                {
                    LocationId = x.LocationId,
                    Name = x.Name,
                    Address = x.Address,
                    Capacity = x.Capacity,
                    Status = x.Status,
                    Type = x.Type,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                }).ToList()
            };

            return View("~/Views/Location/Index.cshtml", vm);
        }

        [Authorize(Roles = "Approver,Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Location/Create.cshtml", new CreateLocationViewModel());
        }

        [Authorize(Roles = "Approver,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLocationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Location/Create.cshtml", vm);
            }

            try
            {
                await _locationService.CreateLocationAsync(new CreateLocationDTO
                {
                    Name = vm.Name,
                    Address = vm.Address ?? string.Empty,
                    Capacity = vm.Capacity,
                    Status = vm.Status,
                    Type = vm.Type,
                    Description = vm.Description ?? string.Empty
                });
                await ExecuteSuccessAsync("Tạo địa điểm thành công.", UserActionType.Create, null, TargetType.None);
                return RedirectToAction(nameof(Index));
            }
            catch (LocationValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Location/Create.cshtml", vm);
        }

        [Authorize(Roles = "Approver,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            return View("~/Views/Location/Edit.cshtml", new UpdateLocationViewModel
            {
                LocationId = location.LocationId,
                Name = location.Name,
                Address = location.Address,
                Capacity = location.Capacity,
                Status = location.Status,
                Type = location.Type,
                Description = location.Description
            });
        }

        [Authorize(Roles = "Approver,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateLocationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Location/Edit.cshtml", vm);
            }

            try
            {
                var updated = await _locationService.UpdateLocationAsync(vm.LocationId, new UpdateLocationDTO
                {
                    Name = vm.Name,
                    Address = vm.Address ?? string.Empty,
                    Capacity = vm.Capacity,
                    Status = vm.Status,
                    Type = vm.Type,
                    Description = vm.Description ?? string.Empty
                });

                if (!updated)
                {
                    return NotFound();
                }

                await ExecuteSuccessAsync("Cập nhật địa điểm thành công.", UserActionType.Update, vm.LocationId, TargetType.None);
                return RedirectToAction(nameof(Index));
            }
            catch (LocationValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Location/Edit.cshtml", vm);
        }

        [Authorize(Roles = "Approver,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                SetError("Id không hợp lệ.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _locationService.DeleteLocationAsync(id);
                if (!result)
                {
                    await ExecuteErrorAsync(new Exception("Địa điểm không tồn tại"), "Địa điểm không tồn tại hoặc đã bị xoá.");
                }
                else
                {
                    await ExecuteSuccessAsync("Xoá địa điểm thành công.", UserActionType.Delete, id, TargetType.None);
                }
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
