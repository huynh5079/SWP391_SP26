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
        public string SpeakerName { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Vui lòng nhập tên tài liệu")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập link tài liệu")]
        public string Url { get; set; } = string.Empty;

        public string? Type { get; set; }
    }
}
