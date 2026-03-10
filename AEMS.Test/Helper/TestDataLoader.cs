using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessLogic.DTOs.Authentication.Register;

namespace AEMS.Test.Helper
{
	internal static class TestDataLoader
	{
		public static RegisterStudentRequestDto LoadStudentRegisterRequest(string fileName, int index = 0)
		{
			var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", fileName));
			var json = File.ReadAllText(path);
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			var items = JsonSerializer.Deserialize<List<StudentRegisterTestData>>(json, options) ?? new List<StudentRegisterTestData>();
			var student = items[index];

			return new RegisterStudentRequestDto
			{
				Email = student.Email,
				Password = student.Password,
				FullName = student.FullName,
				StudentCode = student.StudentCode,
				Phone = student.Phone
			};
		}

		private sealed class StudentRegisterTestData
		{
			[JsonPropertyName("email")]
			public string Email { get; set; } = string.Empty;

			[JsonPropertyName("password")]
			public string Password { get; set; } = string.Empty;

			[JsonPropertyName("Name")]
			public string FullName { get; set; } = string.Empty;

			[JsonPropertyName("mssv")]
			public string StudentCode { get; set; } = string.Empty;

			[JsonPropertyName("sdt")]
			public string Phone { get; set; } = string.Empty;
		}
	}
}
