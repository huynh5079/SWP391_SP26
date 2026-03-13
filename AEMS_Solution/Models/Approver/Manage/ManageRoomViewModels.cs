using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Approver.Manage;

    public class ManageRoomViewModel
    {
        public List<RoomListItemVm> Rooms { get; set; } = new();
        public CreateRoomViewModel NewRoom { get; set; } = new();
    }

    public class RoomListItemVm
    {
        public string LocationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public LocationStatusEnum Status { get; set; }
        public LocationTypeEnum? Type { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CreateRoomViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Building { get; set; }
        public string? Floor { get; set; }
        public string? Room { get; set; }
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
        public LocationStatusEnum Status { get; set; } = LocationStatusEnum.Available;
        [Required]
        public LocationTypeEnum? Type { get; set; }
		public string? Description { get; set; }
    }

    public class UpdateRoomStatusViewModel
    {
        [Required]
        public string LocationId { get; set; } = string.Empty;
        public LocationStatusEnum Status { get; set; }
    }

    public class UpdateRoomViewModel
    {
        [Required]
        public string LocationId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng là bắt buộc.")]
        public string Name { get; set; } = string.Empty;

        public string? Building { get; set; }
        public string? Floor { get; set; }
        public string? Room { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0.")]
        public int Capacity { get; set; }

        public LocationStatusEnum Status { get; set; }

        [Required(ErrorMessage = "Loại phòng là bắt buộc.")]
        public LocationTypeEnum? Type { get; set; }

        public string? Description { get; set; }
    }

