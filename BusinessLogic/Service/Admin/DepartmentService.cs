using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Department;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.Admin
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepartmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<DepartmentDTO>> GetAllDepartmentsAsync()
        {
            var departments = await _unitOfWork.Departments.GetAllAsync(x => x.DeletedAt == null);
            return departments
                .OrderBy(x => x.Name)
                .Select(MapDepartment)
                .ToList();
        }

        public async Task<DepartmentDTO?> GetDepartmentByIdAsync(string departmentId)
        {
            if (string.IsNullOrWhiteSpace(departmentId)) return null;

            var department = await _unitOfWork.Departments.GetAsync(x => x.Id == departmentId && x.DeletedAt == null);
            return department == null ? null : MapDepartment(department);
        }

        public async Task<DepartmentDTO> CreateDepartmentAsync(CreateDepartmentDTO dto)
        {
            var normalizedName = dto.Name.Trim();
            var normalizedCode = dto.Code.Trim();

            var existing = await _unitOfWork.Departments.GetAsync(x => (x.Name == normalizedName || x.Code == normalizedCode) && x.DeletedAt == null);
            if (existing != null)
            {
                throw new InvalidOperationException("Department name or code already exists.");
            }

            var now = DateTimeHelper.GetVietnamTime();
            var department = new DataAccess.Entities.Department
            {
                Id = Guid.NewGuid().ToString(),
                Name = normalizedName,
                Code = normalizedCode,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            await _unitOfWork.Departments.CreateAsync(department);
            await _unitOfWork.SaveChangesAsync();

            return MapDepartment(department);
        }

        public async Task<bool> UpdateDepartmentAsync(string departmentId, UpdateDepartmentDTO dto)
        {
            var department = await _unitOfWork.Departments.GetAsync(x => x.Id == departmentId && x.DeletedAt == null);
            if (department == null)
            {
                throw new InvalidOperationException("Department not found.");
            }

            var normalizedName = dto.Name.Trim();
            var normalizedCode = dto.Code.Trim();

            var duplicate = await _unitOfWork.Departments.GetAsync(x => x.Id != departmentId && (x.Name == normalizedName || x.Code == normalizedCode) && x.DeletedAt == null);
            if (duplicate != null)
            {
                throw new InvalidOperationException("Department name or code already exists.");
            }

            department.Name = normalizedName;
            department.Code = normalizedCode;
            department.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _unitOfWork.Departments.UpdateAsync(department);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteDepartmentAsync(string departmentId)
        {
            var department = await _unitOfWork.Departments.GetAsync(x => x.Id == departmentId && x.DeletedAt == null);
            if (department == null)
            {
                throw new InvalidOperationException("Department not found.");
            }

            // Check if department is in use by events
            var usedEvent = await _unitOfWork.Events.GetAsync(x => x.DepartmentId == departmentId && x.DeletedAt == null);
            if (usedEvent != null)
            {
                throw new InvalidOperationException("Cannot delete department because it is associated with one or more events.");
            }
			
			// Check if used by staff or students
			var usedStaff = await _unitOfWork.StaffProfiles.GetAsync(x => x.DepartmentId == departmentId);
			if(usedStaff != null) throw new InvalidOperationException("Cannot delete department because it is associated with one or more staff profiles.");
			
			var usedStudent = await _unitOfWork.StudentProfiles.GetAsync(x => x.DepartmentId == departmentId);
			if(usedStudent != null) throw new InvalidOperationException("Cannot delete department because it is associated with one or more student profiles.");

            department.DeletedAt = DateTimeHelper.GetVietnamTime();
            await _unitOfWork.Departments.UpdateAsync(department);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static DepartmentDTO MapDepartment(DataAccess.Entities.Department dept)
        {
            return new DepartmentDTO
            {
                Id = dept.Id,
                Name = dept.Name,
                Code = dept.Code,
                CreatedAt = dept.CreatedAt,
                UpdatedAt = dept.UpdatedAt,
                DeletedAt = dept.DeletedAt
            };
        }
    }
}
