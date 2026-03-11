namespace BusinessLogic.DTOs.Department
{
	public class DepartmentDTO
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Code { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public DateTime? DeletedAt { get; set; }
	}

	public class CreateDepartmentDTO
	{
		public string Name { get; set; } = string.Empty;
		public string Code { get; set; } = string.Empty;
	}

	public class UpdateDepartmentDTO
	{
		public string Name { get; set; } = string.Empty;
		public string Code { get; set; } = string.Empty;
	}
}
