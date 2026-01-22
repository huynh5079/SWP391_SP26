namespace BusinessLogic.Service.Interface
{
    public interface ISystemErrorLogService
    {
        Task LogErrorAsync(Exception ex, string? userId, string source);
    }
}
