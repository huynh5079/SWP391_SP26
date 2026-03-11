using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessLogic.DTOs.Authentication.Register;

namespace AEMS.Test.Helper
{
	internal static class TestDataLoader
	{
		public static RegisterStudentRequestDto LoadStudentRegisterRequest(string fileName, int index = 0)
		{
			var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", fileName));
			var json = File.ReadAllText(fullPath);
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
			[JsonPropertyName("Email")]
			public string Email { get; set; } = string.Empty;

			[JsonPropertyName("Password")]
			public string Password { get; set; } = string.Empty;

            // match the JSON keys used in TestData/users.json
            [JsonPropertyName("FullName")]
            public string FullName { get; set; } = string.Empty;

            [JsonPropertyName("StudentCode")]
            public string StudentCode { get; set; } = string.Empty;

            [JsonPropertyName("Phone")]
            public string Phone { get; set; } = string.Empty;
		}
	}
}
