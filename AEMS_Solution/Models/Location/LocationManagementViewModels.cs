using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Location
{
    public class LocationListItemVm
    {
        public string LocationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public LocationStatusEnum Status { get; set; }
        public LocationTypeEnum? Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LocationIndexViewModel
    {
        public string? Search { get; set; }
        public string? FilterStatus { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public List<LocationListItemVm> Locations { get; set; } = new();
    }

    public class CreateLocationViewModel
    {
        [Required(ErrorMessage = "Tên phòng/địa điểm là bắt buộc.")]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Range(1, 100000, ErrorMessage = "Sức chứa phải lớn hơn 0.")]
        public int Capacity { get; set; } = 1;

        public LocationStatusEnum Status { get; set; } = LocationStatusEnum.Available;

        [Required(ErrorMessage = "Loại phòng là bắt buộc.")]
        public LocationTypeEnum? Type { get; set; }

        public string? Description { get; set; }
    }

    public class UpdateLocationViewModel
    {
        [Required]
        public string LocationId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phòng/địa điểm là bắt buộc.")]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Range(1, 100000, ErrorMessage = "Sức chứa phải lớn hơn 0.")]
        public int Capacity { get; set; } = 1;

        public LocationStatusEnum Status { get; set; }

        [Required(ErrorMessage = "Loại phòng là bắt buộc.")]
        public LocationTypeEnum? Type { get; set; }

        public string? Description { get; set; }
    }
}
