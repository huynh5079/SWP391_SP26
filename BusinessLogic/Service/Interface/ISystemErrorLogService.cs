using DataAccess.Entities;

namespace BusinessLogic.Service.Interface
{
    public interface ISystemErrorLogService
    {
        Task LogErrorAsync(Exception ex, string? userId, string source);
        Task<PaginationResult<SystemErrorLog>> GetLogsAsync(int page, int pageSize, string? search, int? statusCode);
        Task DeleteOldLogsAsync(int days = 30);
        Task<(List<string> Dates, List<int> Counts, int TodayCount)> GetErrorTrendAsync(int days = 7);
    }
}
