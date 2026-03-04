using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.ValiDate.ValiDateforApproval
{
	public interface IApprovalValidator
	{
		//Can't Pending 2 times, can't Approve/Reject if not pending, can't Approve/Reject if already Approved/Rejected
		Task<ValidationResult> ValidatePendingAsync(string eventId, string userId);
		Task<ValidationResult> ValidateCommentRejetcAsync(string eventId, string userId);
	}
}
