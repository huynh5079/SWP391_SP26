using System.ComponentModel.DataAnnotations;

namespace AEMS_Solution.Models.Event.Feedback.ForStudentFeedback
{
	public class StudentEventFeedbackViewModel
	{
		public string EventId { get; set; } = string.Empty;
		public string EventTitle { get; set; } = string.Empty;

		[Required]
		[Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
		public int Rating { get; set; } = 5;

		[MaxLength(1000)]
		public string? Comment { get; set; }
	}
}

