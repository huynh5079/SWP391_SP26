using System.ComponentModel.DataAnnotations;

namespace AEMS_Solution.Models.Event
{
    public class CreateDetailAgendaViewModel
    {
        [Required]
        public string EventId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên agenda")]
        public string SessionName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập speaker")]
        public string SpeakerInfo { get; set; } = string.Empty;

        public string? SpeakerUserId { get; set; }
        public string? SpeakerUserRole { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng cho agenda")]
        public string? Location { get; set; }
    }

    public class CreateDetailDocumentViewModel
    {
        [Required]
        public string EventId { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? Url { get; set; }

        public List<Microsoft.AspNetCore.Http.IFormFile>? Files { get; set; }

        public string? Type { get; set; }
    }
}
