using BusinessLogic.DTOs.Role;
using DataAccess.Repositories.Abstraction;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using UserEntity = DataAccess.Entities.User;

namespace BusinessLogic.Service.ValiDateRole.ValiDateforAdmin.LockAndUnlockLimit
{
	public class LockAndUnlockLimitValidator : ILockAndUnlockLimitValidator
	{
		private readonly IUnitOfWork _uow;

		public LockAndUnlockLimitValidator(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<UserEntity> ValidateSetUserLockAsync(AdminSoftDeleteLimitDTO request)
		{
			if (request == null)
			{
				throw new Exception("Dữ liệu khóa tài khoản không hợp lệ.");
			}

			if (string.IsNullOrWhiteSpace(request.Id))
			{
				throw new Exception("Id tài khoản là bắt buộc.");
			}
			
			var user = await _uow.Users.GetByIdAsync(request.Id);
			if (user == null)
			{
				throw new Exception("Không tìm thấy tài khoản.");
			}

			var now = DateTimeHelper.GetVietnamTime();
			if (request.ReactivateAt.HasValue && request.ReactivateAt.Value <= now)
			{
				throw new Exception("Thời gian mở khóa phải lớn hơn thời gian hiện tại.");
			}

			return user;
		}
	}
}
