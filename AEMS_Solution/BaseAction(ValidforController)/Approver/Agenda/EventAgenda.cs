namespace AEMS_Solution.BaseAction_ValidforController_.Approver.Agenda
{
	public class ApproverEventAgendaAction : IApproverEventAgendaAction
	{
		public string EnsureApproverId(string? approverId)
		{
			if (string.IsNullOrWhiteSpace(approverId))
			{
				throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ.");
			}

			return approverId;
		}
	}
}
