using BusinessLogic.DTOs.Event.Topic;
using DataAccess.Entities;

namespace BusinessLogic.Service.ValidationData.Topic
{
	public interface ITopicValidator
	{
		void ValidateCreateRequest(CreateTopicDTO dto);
		void ValidateUpdateRequest(string topicId, UpdateTopicDTO dto);
		void ValidateTopicExists(DataAccess.Entities.Topic? topic);
		void ValidateDuplicateTopic(DataAccess.Entities.Topic? duplicateTopic);
	}
}
