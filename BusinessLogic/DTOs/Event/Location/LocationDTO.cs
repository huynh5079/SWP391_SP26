using System;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Location
{
	public class LocationDTO
	{
		public string LocationId { get; set; } = "";
		public string Name { get; set; } = "";
		public string Address { get; set; } = "";
		public int Capacity { get; set; }
		public LocationStatusEnum Status { get; set; }
		public LocationTypeEnum? Type { get; set; }
		public string Description { get; set; } = "";
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public DateTime DeletedAt { get; set; }
	}

	public class CreateLocationDTO
	{
		public string Name { get; set; } = "";
		public string Address { get; set; } = "";
		public int Capacity { get; set; }
		public LocationStatusEnum Status { get; set; } = LocationStatusEnum.Available;
		public LocationTypeEnum? Type { get; set; }
		public string Description { get; set; } = "";
	}

	public class UpdateLocationDTO
	{
		public string Name { get; set; } = "";
		public string Address { get; set; } = "";
		public int Capacity { get; set; }
		public LocationStatusEnum Status { get; set; }
		public LocationTypeEnum? Type { get; set; }
		public string Description { get; set; } = "";
	}
}
