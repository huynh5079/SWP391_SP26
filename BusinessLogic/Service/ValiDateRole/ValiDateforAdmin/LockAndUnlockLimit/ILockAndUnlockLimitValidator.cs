using BusinessLogic.DTOs.Role;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.ValiDateRole.ValiDateforAdmin.LockAndUnlockLimit
{
	public interface ILockAndUnlockLimitValidator
	{
		Task<UserEntity> ValidateSetUserLockAsync(AdminSoftDeleteLimitDTO request);
	}
}
