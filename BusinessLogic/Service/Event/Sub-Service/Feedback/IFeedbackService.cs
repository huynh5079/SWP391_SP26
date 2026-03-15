using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.EventFeedbackSummary;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback
{
	public interface IFeedBackService
	{
	   Task<EventFeedbackSummaryDto> Spamfeedback(string studentId, string eventId,string comment);
		Task<EventFeedbackSummaryDto> AverageFeedbackRating(string eventId,double ratingScore);
	}
}
