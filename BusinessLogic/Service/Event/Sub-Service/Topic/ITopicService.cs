using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Topic;

namespace BusinessLogic.Service.Event.Sub_Service.Topic
{
	public interface ITopicService
	{
		Task<List<TopicDTO>> GetAllTopicsAsync();
		Task<TopicDTO?> GetTopicByIdAsync(string topicId);
		Task<List<TopicDTO>> GetTopicsByEventAsync(string eventId);
		Task<TopicDTO> CreateTopicAsync(CreateTopicDTO dto);
		Task<bool> UpdateTopicAsync(string topicId, UpdateTopicDTO dto);
	}
}
