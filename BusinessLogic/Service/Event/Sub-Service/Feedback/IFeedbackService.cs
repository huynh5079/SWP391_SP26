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

		
		//check spam
		Task<EventFeedbackSummaryDto> CreateFeedback(string studentId, string eventId, string? comment, double rating);
		Task<bool> HasStudentFeedback(string studentId, string eventId);
		Task<List<EventFeedbackSummaryDto>> GetFeedbacksByEvent(string eventId);
		Task<EventFeedbackSummaryDto> GetEventFeedbackSummary(string eventId);
		Task DeleteFeedback(string feedbackId);
		//include rating and comment
		Task<EventFeedbackSummaryDto>UpdateFeedback(string feedbackId, string? comment, double rating);
		Task<List<EventTopRatingDto>> GetTopRatedEvents(int top);
		Task<int> AnalyzeEventFeedbacksAsync(string eventId);

	}
}
