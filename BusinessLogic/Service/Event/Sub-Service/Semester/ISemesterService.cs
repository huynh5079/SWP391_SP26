using BusinessLogic.DTOs.Event.Semester;
using DataAccess.Enum;

namespace BusinessLogic.Service.Event.Sub_Service.Semester
{
   public interface ISemesterService
	{
		Task<List<SemesterDTO>> GetAllSemestersAsync();
		Task<SemesterDTO?> GetSemesterByIdAsync(string semesterId);
		Task<SemesterDTO> CreateSemesterAsync(SemesterDTO dto);
        Task<SemesterDTO> AutoCreateSemesterAsync();
		Task<bool> UpdateSemesterAsync(string semesterId, SemesterDTO dto);
		Task<bool> DeleteSemesterAsync(string semesterId);
	}
}
