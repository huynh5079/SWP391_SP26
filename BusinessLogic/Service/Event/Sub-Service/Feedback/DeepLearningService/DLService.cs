using System.Net.Http.Json;
using BusinessLogic.DTOs.Event.EventFeedbackSummary;

namespace BusinessLogic.Service.Event.Sub_Service.Feedback.DeepLearningService
{
	public class DLService
	{
		private readonly HttpClient _httpClient;

		public DLService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_httpClient.BaseAddress = new Uri("http://localhost:8011"); // Use a specific port to avoid conflicts
		}

		public async Task<EventFeedbackAPIDTO?> AnalyzeFeedbackAsync(string comment, string eventId)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("/predict", new { comment, eventId });
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<EventFeedbackAPIDTO>();
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
