using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.EventFeedbackSummary;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback.DeepLearningService
{
	public interface IDLService
	{
		Task<EventFeedbackAPIDTO?> AnalyzeFeedbackAsync(string comment, string eventId);
	}
}
