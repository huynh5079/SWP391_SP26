using DataAccess.Repositories.Abstraction;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.ValiDateRole.ValidateUser
{
	public class UserValidator : IUserValidator
	{
		private readonly IUnitOfWork _uow;

		public UserValidator(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<UserEntity> ValidateCheckContainIdAsync(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new Exception("Id tài khoản là bắt buộc.");
			}

			var user = await _uow.Users.GetByIdAsync(id);
			if (user == null)
			{
				throw new Exception("Không tìm thấy tài khoản.");
			}

			return user;
		}
	}
}
