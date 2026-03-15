using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.EventFeedbackSummary
{
	
		public class EventFeedbackSummaryDto
		{
			public string EventId { get; set; } = "";
			public string EventTitle { get; set; } = "";
			public double Rating { get; set; }
			public string? Comment { get; set; }
			public DateTime? CreatedAt { get; set; }
			public string? StudentId { get; set; }
			public string? StudentCode { get; set; }
		}
	
}
