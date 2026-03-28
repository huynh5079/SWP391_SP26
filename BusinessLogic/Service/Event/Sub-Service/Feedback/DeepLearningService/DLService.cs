using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using BusinessLogic.DTOs.Event.EventFeedbackSummary;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback.DeepLearningService
{
	public class DLService : IDLService
	{
		private readonly IConfiguration _configuration;

		public DLService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			
			var baseUrl = _configuration["AppSettings:AiEngineUrl"]?.TrimEnd('/') ?? "http://localhost:8011";
			_httpClient.BaseAddress = new Uri(baseUrl);
		}

		public async Task<EventFeedbackAPIDTO?> AnalyzeFeedbackAsync(string comment, string eventId)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("/predict", new { comment, eventId });
				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[DLService] Raw JSON: {content}");
					
					using var doc = JsonDocument.Parse(content);
					var root = doc.RootElement;
					
					// Case-insensitive helper to get int? from property
					int? GetInt(JsonElement el, params string[] names) {
						foreach (var name in names) {
							if (el.TryGetProperty(name, out var prop)) return prop.GetInt32();
							// try lowercase
							if (el.TryGetProperty(name.ToLower(), out var lowProp)) return lowProp.GetInt32();
						}
						return null;
					}

					string? GetString(JsonElement el, params string[] names) {
						foreach (var name in names) {
							if (el.TryGetProperty(name, out var prop)) return prop.GetString();
							if (el.TryGetProperty(name.ToLower(), out var lowProp)) return lowProp.GetString();
						}
						return null;
					}

					var dto = new EventFeedbackAPIDTO
					{
						EventId = GetString(root, "EventId", "event_id") ?? "",
						Comment = GetString(root, "Comment", "comment") ?? "",
						Label = GetInt(root, "Label"),
						Technical = GetInt(root, "Technical"),
						Content = GetInt(root, "Content"),
						Instructor = GetInt(root, "Instructor"),
						Assessment = GetInt(root, "Assessment", "Asessment"), // Fallback for typo
						Label_Text = GetString(root, "Label_Text", "label_text"),
						Technical_Text = GetString(root, "Technical_Text", "technical_text"),
						Content_Text = GetString(root, "Content_Text", "content_text"),
						Instructor_Text = GetString(root, "Instructor_Text", "instructor_text"),
						Assessment_Text = GetString(root, "Assessment_Text", "assessment_text", "asessment_text")
					};
					
					Console.WriteLine($"[DLService] Mapped DTO: Label={dto.Label}, Tech={dto.Technical}, Content={dto.Content}, Inst={dto.Instructor}, Assess={dto.Assessment}");
					return dto;
				}
				return null;
			}
			catch (Exception)
			{
				// Fail silently
				return null;
			}
		}
	}
}
