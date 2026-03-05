using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using static System.Collections.Specialized.BitVector32;

namespace BusinessLogic.Service.ValiDate.ValiDateforApproval
{
	/*internal class ApprovalValidator : IApprovalValidator
	{
		private readonly DataAccess.Repositories.Abstraction.IUnitOfWork _uow;
		public ApprovalValidator(IUnitOfWork uow) {
		     _uow = uow;
		}
		public async Task<ValidationResult> ValidateCommentRejetcAsync(string eventId, string userId)
		{
			var lastLog = await _uow.ApprovalLogs.GetAllAsync(x => x.EventId == eventId);

			if (lastLog.Any())
				return InvalidOperationException("Event already reviewed");
		}

		public async Task<ValidationResult> ValidatePendingAsync(string eventId, string userId)
		{
			if (action == ApprovalActionEnum.Reject && string.IsNullOrWhiteSpace(comment))
				throw new ValidationException("Reject must include comment");
		}
	}*/
}
