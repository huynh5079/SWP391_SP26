using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BusinessLogic.DTOs.Event.EventFeedbackSummary;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback
{
	public class FeedbackService : IFeedBackService
	{
      private readonly IUnitOfWork _unitOfWork;

		public FeedbackService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		

       public async Task<EventFeedbackSummaryDto> CreateFeedback(string studentId, string eventId, string? comment, double rating)
		{
            if (string.IsNullOrWhiteSpace(studentId))
			{
				throw new InvalidOperationException("StudentId không du?c d? tr?ng.");
			}

			if (string.IsNullOrWhiteSpace(eventId))
			{
				throw new InvalidOperationException("EventId không du?c d? tr?ng.");
			}

			if (rating < 1 || rating > 5)
			{
				throw new InvalidOperationException("Rating ph?i trong kho?ng t? 1 d?n 5.");
			}

			var student = await _unitOfWork.StudentProfiles.GetAsync(x => x.Id == studentId && x.DeletedAt == null);
			if (student == null)
			{
				throw new InvalidOperationException("Sinh viên không t?n t?i.");
			}

			var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == eventId && x.DeletedAt == null);
			if (eventEntity == null)
			{
				throw new InvalidOperationException("S? ki?n không t?n t?i.");
			}

			var existingFeedback = await _unitOfWork.Feedbacks.GetAsync(
				x => x.EventId == eventId && x.StudentId == studentId && x.DeletedAt == null);

			if (existingFeedback != null)
			{
				throw new InvalidOperationException("Sinh viên dã feedback cho s? ki?n này.");
			}

			var feedback = new DataAccess.Entities.Feedback
			{
				EventId = eventId,
				StudentId = studentId,
				Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
				Status = ResolveFeedbackStatus(eventEntity),
				RatingEvent = ResolveRatingEnum(rating)
			};

			await _unitOfWork.Feedbacks.CreateAsync(feedback);
			await _unitOfWork.SaveChangesAsync();

			feedback.Event = eventEntity;
			feedback.Student = student;

			return MapFeedback(feedback);
		}

       public async Task DeleteFeedback(string feedbackId)
		{
            if (string.IsNullOrWhiteSpace(feedbackId))
			{
				throw new InvalidOperationException("FeedbackId không du?c d? tr?ng.");
			}

			var feedback = await _unitOfWork.Feedbacks.GetAsync(x => x.Id == feedbackId && x.DeletedAt == null);
			if (feedback == null)
			{
				throw new InvalidOperationException("Feedback không t?n t?i.");
			}

			await _unitOfWork.Feedbacks.RemoveAsync(feedback);
			await _unitOfWork.SaveChangesAsync();
		}

        public async Task<EventFeedbackSummaryDto> GetEventFeedbackSummary(string eventId)
		{
            if (string.IsNullOrWhiteSpace(eventId))
			{
				throw new InvalidOperationException("EventId không du?c d? tr?ng.");
			}

			var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == eventId && x.DeletedAt == null);
			if (eventEntity == null)
			{
				throw new InvalidOperationException("S? ki?n không t?n t?i.");
			}

			var feedbacks = (await _unitOfWork.Feedbacks.GetAllAsync(x => x.EventId == eventId && x.DeletedAt == null))
				.OrderByDescending(x => x.CreatedAt)
				.ToList();

			var latest = feedbacks.FirstOrDefault();
			return new EventFeedbackSummaryDto
			{
				EventId = eventEntity.Id,
				EventTitle = eventEntity.Title,
                Rating = feedbacks.Count == 0 ? 0 : Math.Round(feedbacks.Average(x => (double)(int)x.RatingEvent), 2),
				Comment = latest?.Comment,
				CreatedAt = latest?.CreatedAt
			};
		}

      public async Task<List<EventFeedbackSummaryDto>> GetFeedbacksByEvent(string eventId)
		{
            if (string.IsNullOrWhiteSpace(eventId))
			{
				return new List<EventFeedbackSummaryDto>();
			}

			var feedbacks = await _unitOfWork.Feedbacks.GetAllAsync(
				x => x.EventId == eventId && x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student));

			return feedbacks
				.OrderByDescending(x => x.CreatedAt)
				.Select(MapFeedback)
				.ToList();
		}

     public async Task<List<EventTopRatingDto>> GetTopRatedEvents(int top)
		{
            if (top <= 0)
			{
				return new List<EventTopRatingDto>();
			}

			var feedbacks = await _unitOfWork.Feedbacks.GetAllAsync(
				x => x.DeletedAt == null,
				q => q.Include(x => x.Event));

			return feedbacks
				.Where(x => x.Event != null)
				.GroupBy(x => new { EventId = x.EventId!, EventTitle = x.Event!.Title })
				.Select(g => new EventTopRatingDto
				{
					EventId = g.Key.EventId,
					EventTitle = g.Key.EventTitle,
                 AverageRating = Math.Round(g.Average(x => (double)(int)x.RatingEvent), 2)
				})
				.OrderByDescending(x => x.AverageRating)
				.ThenBy(x => x.EventTitle)
				.Take(top)
				.ToList();
		}

      public async Task<bool> HasStudentFeedback(string studentId, string eventId)
		{
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(eventId))
			{
				return false;
			}

			var existing = await _unitOfWork.Feedbacks.GetAsync(
				x => x.StudentId == studentId && x.EventId == eventId && x.DeletedAt == null);

			return existing != null;
		}

     public async Task<EventFeedbackSummaryDto> UpdateFeedback(string feedbackId, string? comment, double rating)
		{
            if (string.IsNullOrWhiteSpace(feedbackId))
			{
				throw new InvalidOperationException("FeedbackId không du?c d? tr?ng.");
			}

			if (rating < 1 || rating > 5)
			{
				throw new InvalidOperationException("Rating ph?i trong kho?ng t? 1 d?n 5.");
			}

			var feedback = await _unitOfWork.Feedbacks.GetAsync(
				x => x.Id == feedbackId && x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student));

			if (feedback == null)
			{
				throw new InvalidOperationException("Feedback không t?n t?i.");
			}

			feedback.RatingEvent = (FeedBackRatingsEnum)rating;
			feedback.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
			feedback.RatingEvent = ResolveRatingEnum(rating);
			feedback.Status = feedback.Event == null ? FeedbackStatusEnum.NA : ResolveFeedbackStatus(feedback.Event);
			feedback.UpdatedAt = DateTimeHelper.GetVietnamTime();

			await _unitOfWork.Feedbacks.UpdateAsync(feedback);
			await _unitOfWork.SaveChangesAsync();

			return MapFeedback(feedback);
		}

		private static EventFeedbackSummaryDto MapFeedback(DataAccess.Entities.Feedback feedback)
		{
			return new EventFeedbackSummaryDto
			{
				EventId = feedback.EventId ?? string.Empty,
				EventTitle = feedback.Event?.Title ?? string.Empty,
               Rating = (int)feedback.RatingEvent,
				Comment = feedback.Comment,
				CreatedAt = feedback.CreatedAt,
				StudentId = feedback.StudentId,
				StudentCode = feedback.Student?.StudentCode
			};
		}

		private static FeedBackRatingsEnum ResolveRatingEnum(double rating)
		{
			var rounded = Math.Clamp((int)Math.Round(rating, MidpointRounding.AwayFromZero), 1, 5);
			return (FeedBackRatingsEnum)rounded;
		}

        private static FeedbackStatusEnum ResolveFeedbackStatus(DataAccess.Entities.Event eventEntity)
		{
			var now = DateTimeHelper.GetVietnamTime();
			if (now >= eventEntity.StartTime && now <= eventEntity.EndTime)
			{
				return FeedbackStatusEnum.DuringEvent;
			}

			if (now > eventEntity.EndTime)
			{
				return FeedbackStatusEnum.AfterEvent;
			}

           return FeedbackStatusEnum.BeforeEvent;
		}

		private static AppriciateEventEnum ResolveAppriciateLevel(double averageRating)
		{
			if (averageRating >= 4)
			{
				return AppriciateEventEnum.Positive;
			}

			if (averageRating >= 3)
			{
				return AppriciateEventEnum.Neutral;
			}

			return AppriciateEventEnum.Negative;
		}
	}
}

