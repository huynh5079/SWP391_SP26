using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace AEMS_Solution.Models.Event
{
    public class UpdateEventViewModel
    {
        // ID của Event cần cập nhật (Hidden field trên Form)
        [Required]
        public string EventId { get; set; } = "";

        [Required(ErrorMessage = "Tiêu đề không được để trống"), StringLength(500)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? ThumbnailFile { get; set; }
        public List<Microsoft.AspNetCore.Http.IFormFile>? BannerFiles { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Semester")]
        public string SemesterId { get; set; } = "";

        public string? DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Location")]
        public string LocationId { get; set; } = "";

        public string? TopicId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndTime { get; set; }

        [Range(1, 200000, ErrorMessage = "Sức chứa phải từ 1 đến 200,000")]
        public int MaxCapacity { get; set; }

        public bool IsDepositRequired { get; set; }

        [Range(0, 999999999, ErrorMessage = "Tiền cọc không hợp lệ")]
        public decimal DepositAmount { get; set; }

        public EventTypeEnum Type { get; set; }

        public EventModeEnum Mode { get; set; }
        public string? MeetingUrl { get; set; }

        // Trạng thái hiện tại của Event (Upcoming, Draft, Cancelled...)
        public EventStatusEnum Status { get; set; }

        // Dropdown sources (Dùng để render lại Select list khi sửa)
        public List<SelectListItem> Semesters { get; set; } = new();
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Locations { get; set; } = new();
        public List<SelectListItem> Topics { get; set; } = new();

        // Danh sách Agenda (Lịch trình) hiện có của Event
        public List<UpdateAgendaItemVm> Agendas { get; set; } = new();
        // Documents
        public List<UpdateDocumentVm> Documents { get; set; } = new();
    }

    public class UpdateAgendaItemVm
    {
        // Nếu Agenda đã tồn tại trong DB thì cần Id để Update thay vì Insert mới
        public string? Id { get; set; }

        public string? SessionName { get; set; }
        public string? Description { get; set; }
        public string? SpeakerInfo { get; set; }
        public string? SpeakerUserId { get; set; }
        public string? SpeakerUserRole { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Location { get; set; }
    }

    public class UpdateDocumentVm
    {
        public string? Id { get; set; }
        public string? FileName { get; set; }
        public string? Url { get; set; }
        public string? Type { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
    }
	public class SoftDeleteEventViewModel
	{
		[Required]
		public string EventId { get; set; } = string.Empty;

		// NotAvailable = soft delete; Available = restore
		[Required]
		public EventStatusAvailableEnum StatusEventAvailable { get; set; }
	}
}
