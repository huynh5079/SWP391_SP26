using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Admin;
using BusinessLogic.DTOs.Department;
using BusinessLogic.Service.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                SetSuccess("Tạo phòng ban mới thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetError($"Có lỗi xảy ra: {ex.Message}");
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
                SetSuccess("Cập nhật phòng ban thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetError($"Có lỗi xảy ra: {ex.Message}");
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
                SetSuccess("Đã xóa phòng ban thành công.");
            }
            catch (Exception ex)
            {
                SetError($"Không thể xóa: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
