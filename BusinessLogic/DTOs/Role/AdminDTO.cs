using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Role
{
	public class AdminSoftDeleteLimitDTO
	{
		[Required]
		public string Id { get; set; } = string.Empty;

		public UserStatusEnum Status { get; set; } = UserStatusEnum.Inactive;

		[DataType(DataType.DateTime)]
		public DateTime? ReactivateAt { get; set; }

		public bool IsSoftDeleted => Status == UserStatusEnum.Inactive;

		public bool HasAutoReactivate => ReactivateAt.HasValue;
	}
}
