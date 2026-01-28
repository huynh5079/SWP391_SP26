using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Entities;

namespace BusinessLogic.Service.Interface
{
    public interface ISystemErrorLogService
    {
        Task LogErrorAsync(Exception ex, string? userId, string source, DataAccess.Enum.SystemLogStatusEnum? statusCode = DataAccess.Enum.SystemLogStatusEnum.ServerError);
        Task<PaginationResult<SystemErrorLog>> GetLogsAsync(int page, int pageSize, string? search, DataAccess.Enum.SystemLogStatusEnum? statusCode);
        Task DeleteOldLogsAsync(int days = 30);
        Task<(List<string> Dates, List<int> Counts, int TodayCount)> GetErrorTrendAsync(int days = 7);
    }
}
