using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Enum;
using System.Text.Json.Serialization;

namespace BusinessLogic.DTOs.Event.EventFeedbackSummary
{

	public class EventFeedbackSummaryDto
	{
		public string EventId { get; set; } = "";
		public string EventTitle { get; set; } = "";
		/// <summary>Rating của từng feedback riêng lẻ (1-5)</summary>
		public FeedBackRatingsEnum? Rating { get; set; }
		/// <summary>Điểm trung bình toàn event – computed, không lưu DB</summary>
		public double AvgRating { get; set; }
		public string? Comment { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string? StudentId { get; set; }
		public string? StudentCode { get; set; }
	}
	public class EventFeedbackAnalysisDto
	{
		public string EventId { get; set; } = "";
		public string EventTitle { get; set; } = "";
		public int TotalFeedbacks { get; set; }
		public FeedBackRatingsEnum AverageRating { get; set; }

		public FeedbackStatusEnum Status { get; set; }

		public int CommentCount { get; set; }
		public DateTime? LatestFeedbackAt { get; set; }
		public AppriciateEventEnum AppriciateLevel { get; set; }
	}
	public class EventTopRatingDto
	{
		public string EventId { get; set; } = "";
		public string EventTitle { get; set; } = "";
		public double AverageRating { get; set; }
	}
	public class EventFeedbackAPIDTO
	{
		public string EventId { get; set; } = "";
		public string Comment { get; set; } = "";
		public int? Label { get; set; }
		public int? Technical { get; set; }
		public int? Content { get; set; }
		public int? Instructor { get; set; }
		public int? Assessment { get; set; }
		public string? Label_Text { get; set; }
		public string? Technical_Text { get; set; }
		public string? Content_Text { get; set; }
		public string? Instructor_Text { get; set; }
		public string? Assessment_Text { get; set; }
	}

	/// <summary>DTO dùng để trả về toàn bộ feedback của một sự kiện cho Organizer.</summary>
	public class FeedbackForOrganizerDTO
	{
		public string? EventId { get; set; }
		public string? EventTitle { get; set; }
		public int TotalFeedbacks { get; set; }
		public double AverageRating { get; set; }
		public List<EventFeedbackSummaryDto> Feedbacks { get; set; } = new();
	}
}
