using System;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.QuizForAll
{
    public class QuizTimeLimitDto
    {
        public int? TimeLimitMinutes { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsTimedOut { get; set; }

        public SubmissionStatus SubmitStatus{ get; set; }

        public int AttemptNumber { get; set; }
        public int? MaxAttempts { get; set; }
	}
}
