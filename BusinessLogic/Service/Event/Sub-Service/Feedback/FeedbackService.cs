using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.EventFeedbackSummary;


namespace BusinessLogic.Service.Event.Sub_Service.Feedback
{
	public class FeedbackService : IFeedBackService
	{
		public Task<EventFeedbackSummaryDto> AverageFeedbackRating(string eventId, double ratingScore)
		{
			throw new NotImplementedException();
		}

		public Task<EventFeedbackSummaryDto> Spamfeedback(string studentId, string eventId, string comment)
		{
			throw new NotImplementedException();
		}
	}
}
