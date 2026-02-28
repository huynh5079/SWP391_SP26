using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.Service.Approval
{
	
	
	public interface IApproverQueryService
	{
		Task<List<EventItemDto>> GetPendingEventsAsync(string? search, string? status, int page=1, int pageSize=10);
		Task<ApproverEventDetailDto?> GetEventDetailAsync(string eventId);
        // 4) Chỉ lấy logs (nếu UI có popup log)
		Task<List<ApprovalLogDto>> GetApprovalLogsAsync(string eventId);
	}

	public interface IApproverCommandService
	{
        // 3) Action duyệt
		
		Task ApproveAsync(string eventId, string approverId, string? comment);
		Task RejectAsync(string eventId, string approverId, string? comment);
		Task RequestChangeAsync(string eventId, string approverId, string? comment);
	}
}
