using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.ValiDateRole.ValidateUser
{
	public interface IUserValidator
	{
		Task<UserEntity> ValidateCheckContainIdAsync(string id);
	}
}
