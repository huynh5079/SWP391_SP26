using System;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Admin
{
    public class AdminFeedbackViewModel
    {
        public string Id { get; set; } = null!;
        public string EventName { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string StudentCode { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public FeedbackStatusEnum Status { get; set; }
        public FeedBackRatingsEnum? Rating { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
