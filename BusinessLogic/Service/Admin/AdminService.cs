using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role;
using BusinessLogic.Service.Admin.Interface;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.Admin
{
	public class AdminService : IAdminService
	{
		private readonly IUnitOfWork _uow;

		public AdminService(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public async Task<AdminSoftDeleteLimitDTO> RequestChangeStatusLimtAsync(string userId)
		{
			var account = await _uow.Users.GetByIdAsync(userId);
			if (account == null)
			{
				throw new Exception("Không tìm thấy tài khoản.");
			}

			return new AdminSoftDeleteLimitDTO
			{
				Id = account.Id,
				Status = account.Status ?? UserStatusEnum.Inactive,
				ReactivateAt = account.ReactivateAt
			};
		}
	}
}
