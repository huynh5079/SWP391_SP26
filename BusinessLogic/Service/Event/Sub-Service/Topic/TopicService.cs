using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Topic;
using BusinessLogic.Service.ValidationData.Topic;
using DataAccess.Repositories.Abstraction;
using DataAccess.Helper;

namespace BusinessLogic.Service.Event.Sub_Service.Topic
{
	public class TopicService : ITopicService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ITopicValidator _topicValidator;

		public TopicService(IUnitOfWork unitOfWork, ITopicValidator topicValidator)
		{
			_unitOfWork = unitOfWork;
			_topicValidator = topicValidator;
		}

		public async Task<List<TopicDTO>> GetAllTopicsAsync()
		{
			var topics = await _unitOfWork.Topics.GetAllAsync(x => x.DeletedAt == null);
			return topics
				.OrderBy(x => x.Name)
				.Select(MapTopic)
				.ToList();
		}

		public async Task<TopicDTO?> GetTopicByIdAsync(string topicId)
		{
			if (string.IsNullOrWhiteSpace(topicId))
			{
				return null;
			}

			var topic = await _unitOfWork.Topics.GetAsync(x => x.Id == topicId && x.DeletedAt == null);
			return topic == null ? null : MapTopic(topic);
		}

		public async Task<List<TopicDTO>> GetTopicsByEventAsync(string eventId)
		{
			if (string.IsNullOrWhiteSpace(eventId))
			{
				return new List<TopicDTO>();
			}

			var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == eventId && x.DeletedAt == null);
			if (eventEntity?.TopicId == null)
			{
				return new List<TopicDTO>();
			}

			var topic = await GetTopicByIdAsync(eventEntity.TopicId);
			return topic == null ? new List<TopicDTO>() : new List<TopicDTO> { topic };
		}

		public async Task<TopicDTO> CreateTopicAsync(CreateTopicDTO dto)
		{
			_topicValidator.ValidateCreateRequest(dto);

			var normalizedName = dto.TopicName.Trim();
			var existingTopic = await _unitOfWork.Topics.GetAsync(x => x.Name == normalizedName && x.DeletedAt == null);
			_topicValidator.ValidateDuplicateTopic(existingTopic);

			var now = DateTimeHelper.GetVietnamTime();
			var topic = new DataAccess.Entities.Topic
			{
				Id = Guid.NewGuid().ToString(),
				Name = normalizedName,
				Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
				CreatedAt = now,
				UpdatedAt = now,
				DeletedAt = null
			};

			await _unitOfWork.Topics.CreateAsync(topic);
			await _unitOfWork.SaveChangesAsync();
			return MapTopic(topic);
		}

		public async Task<bool> UpdateTopicAsync(string topicId, UpdateTopicDTO dto)
		{
			try
			{
				_topicValidator.ValidateUpdateRequest(topicId, dto);
			}
			catch (TopicValidator.BusinessValidationException)
			{
				return false;
			}

			var topic = await _unitOfWork.Topics.GetAsync(x => x.Id == topicId && x.DeletedAt == null);
			try
			{
				_topicValidator.ValidateTopicExists(topic);
			}
			catch (TopicValidator.BusinessValidationException)
			{
				return false;
			}

			var normalizedName = dto.TopicName.Trim();
			var duplicateTopic = await _unitOfWork.Topics.GetAsync(x => x.Id != topicId && x.Name == normalizedName && x.DeletedAt == null);
			_topicValidator.ValidateDuplicateTopic(duplicateTopic);

			topic!.Name = normalizedName;
			topic.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
			topic.UpdatedAt = DateTimeHelper.GetVietnamTime();

			await _unitOfWork.Topics.UpdateAsync(topic);
			await _unitOfWork.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteTopicAsync(string topicId)
		{
			var topic = await _unitOfWork.Topics.GetAsync(x => x.Id == topicId && x.DeletedAt == null);
			try
			{
				_topicValidator.ValidateTopicExists(topic);
			}
			catch (TopicValidator.BusinessValidationException)
			{
				return false;
			}

			var usedEvent = await _unitOfWork.Events.GetAsync(x => x.TopicId == topicId && x.DeletedAt == null);
			_topicValidator.ValidateTopicNotUsed(usedEvent != null);

			topic!.DeletedAt = DateTimeHelper.GetVietnamTime();
			await _unitOfWork.Topics.UpdateAsync(topic);
			await _unitOfWork.SaveChangesAsync();

			return true;
		}

		private static TopicDTO MapTopic(DataAccess.Entities.Topic topic)
		{
			return new TopicDTO
			{
				TopicId = topic.Id,
				TopicName = topic.Name,
				Description = topic.Description ?? string.Empty,
				CreatedAt = topic.CreatedAt,
				UpdatedAt = topic.UpdatedAt,
				DeletedAt = topic.DeletedAt ?? DateTime.MinValue
			};
		}
	}
}
