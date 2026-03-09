using System.ComponentModel.DataAnnotations;

namespace AEMS_Solution.Models.Approver
{
    public class TopicListItemVm
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TopicIndexViewModel
    {
        public string? Search { get; set; }
        public List<TopicListItemVm> Topics { get; set; } = new();
    }

    public class CreateTopicViewModel
    {
        [Required]
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateTopicViewModel
    {
        [Required]
        public string TopicId { get; set; } = string.Empty;

        [Required]
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
