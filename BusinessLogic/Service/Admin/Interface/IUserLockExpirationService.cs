namespace BusinessLogic.Service.Admin.Interface
{
    public interface IUserLockExpirationService
    {
        Task<int> ProcessExpiredUserLocksAsync(CancellationToken cancellationToken = default);
    }
}
