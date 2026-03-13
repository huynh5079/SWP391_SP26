using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Helper;
namespace BusinessLogic.DTOs.Event.Topic
{
	public class TopicDTO
	{
		public string TopicId { get; set; } = "";
		public string TopicName { get; set; } = "";
		public string Description { get; set; } = "";
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public DateTime DeletedAt { get; set; }

	}
	public class CreateTopicDTO
	{
		public string TopicName { get; set; } = "";
		public string Description { get; set; } = "";
	}
	public class UpdateTopicDTO
	{
		public string TopicName { get; set; } = "";
		public string Description { get; set; } = "";
	}
}
