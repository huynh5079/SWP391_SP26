using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role;

namespace BusinessLogic.Service.Admin.Interface
{
	public interface IAdminService
	{
		Task<AdminSoftDeleteLimitDTO> RequestChangeStatusLimtAsync(string userId);
	}
}
