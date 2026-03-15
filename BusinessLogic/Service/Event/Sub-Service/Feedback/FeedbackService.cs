using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback
{
	internal class FeedbackService
	{
<<<<<<< HEAD
=======
		public Task<EventFeedbackAnalysisDto> AnalyzeEventFeedback(string eventId)
		{
			throw new NotImplementedException();
		}

		public Task<EventFeedbackSummaryDto> CreateFeedback(string studentId, string eventId, string? comment, double rating)
		{
			throw new NotImplementedException();
		}

		public Task DeleteFeedback(string feedbackId)
		{
			throw new NotImplementedException();
		}

		public Task<EventFeedbackSummaryDto> GetEventFeedbackSummary(string eventId)
		{
			throw new NotImplementedException();
		}

		public Task<List<EventFeedbackSummaryDto>> GetFeedbacksByEvent(string eventId)
		{
			throw new NotImplementedException();
		}

		public Task<List<EventTopRatingDto>> GetTopRatedEvents(int top)
		{
			throw new NotImplementedException();
		}

		public Task<bool> HasStudentFeedback(string studentId, string eventId)
		{
			throw new NotImplementedException();
		}

		public Task<EventFeedbackSummaryDto> Spamfeedback(string studentId, string eventId, string comment)
		{
			throw new NotImplementedException();
		}

		public Task<EventFeedbackSummaryDto> UpdateFeedback(string feedbackId, string? comment, double rating)
		{
			throw new NotImplementedException();
		}
>>>>>>> 63-feat-Feedback-Event
	}
}
