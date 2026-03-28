using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Admin;
using BusinessLogic.DTOs.Department;
using BusinessLogic.Service.Event.EventDepartment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;

namespace AEMS_Solution.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : BaseController
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            var model = new DepartmentIndexViewModel
            {
                Departments = departments.Select(d => new DepartmentViewModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code
                }).ToList()
            };
            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var dto = new CreateDepartmentDTO
                {
                    Name = model.Name,
                    Code = model.Code
                };

                await _departmentService.CreateDepartmentAsync(dto);
                await ExecuteSuccessAsync("Tạo phòng ban mới thành công.", UserActionType.Create, dto.Code, TargetType.Department);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var dept = await _departmentService.GetDepartmentByIdAsync(id);
            if (dept == null) return NotFound();

            var model = new DepartmentEditViewModel
            {
                Id = dept.Id,
                Name = dept.Name,
                Code = dept.Code
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, DepartmentEditViewModel model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var dto = new UpdateDepartmentDTO
                {
                    Name = model.Name,
                    Code = model.Code
                };

                await _departmentService.UpdateDepartmentAsync(id, dto);
                await ExecuteSuccessAsync("Cập nhật phòng ban thành công.", UserActionType.Update, id, TargetType.Department);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            try
            {
                await _departmentService.DeleteDepartmentAsync(id);
                await ExecuteSuccessAsync("Đã xóa phòng ban thành công.", UserActionType.Delete, id, TargetType.Department);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
