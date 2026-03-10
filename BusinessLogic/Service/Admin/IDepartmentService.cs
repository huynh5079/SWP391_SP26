using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Department;

namespace BusinessLogic.Service.Admin
{
    public interface IDepartmentService
    {
        Task<List<DepartmentDTO>> GetAllDepartmentsAsync();
        Task<DepartmentDTO?> GetDepartmentByIdAsync(string departmentId);
        Task<DepartmentDTO> CreateDepartmentAsync(CreateDepartmentDTO dto);
        Task<bool> UpdateDepartmentAsync(string departmentId, UpdateDepartmentDTO dto);
        Task<bool> DeleteDepartmentAsync(string departmentId);
    }
}
