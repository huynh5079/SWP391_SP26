using System;
using BusinessLogic.DTOs.Event.Topic;

namespace BusinessLogic.Service.ValidationData.Topic
{
	public class TopicValidator : ITopicValidator
	{
		public void ValidateCreateRequest(CreateTopicDTO dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.TopicName))
			{
				throw new BusinessValidationException("Tên topic không được để trống.");
			}
		}

		public void ValidateUpdateRequest(string topicId, UpdateTopicDTO dto)
		{
			if (string.IsNullOrWhiteSpace(topicId) || dto == null || string.IsNullOrWhiteSpace(dto.TopicName))
			{
				throw new BusinessValidationException("Dữ liệu cập nhật topic không hợp lệ.");
			}
		}

		public void ValidateTopicExists(DataAccess.Entities.Topic? topic)
		{
			if (topic == null)
			{
				throw new BusinessValidationException("Topic không tồn tại.");
			}
		}

		public void ValidateDuplicateTopic(DataAccess.Entities.Topic? duplicateTopic)
		{
			if (duplicateTopic != null)
			{
				throw new BusinessValidationException("Topic đã tồn tại.");
			}
		}

		public class BusinessValidationException : Exception
		{
			public BusinessValidationException(string message) : base(message)
			{
			}
		}
	}
}
