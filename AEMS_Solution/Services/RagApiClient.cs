using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AEMS_Solution.Services
{
    /// <summary>
    /// RAG API Client for calling the Python RAG server
    /// </summary>
    public class RagApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public RagApiClient(string baseUrl = "http://localhost:8000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Check if RAG API server is running
        /// </summary>
        public async Task<HealthResponse> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HealthResponse>(json, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"RAG API server not accessible at {_baseUrl}", ex);
            }
        }

        /// <summary>
        /// Ask a question and get JSON response (better for reading full answer at once)
        /// </summary>
        public async Task<AskResponse> AskJsonAsync(string question, int topK = 5, string role = "user")
        {
            var request = new AskRequest
            {
                Question = question,
                TopK = topK,
                Role = role
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/ask", content);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AskResponse>(jsonResponse, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling RAG API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ask a question and get streaming response (token by token)
        /// </summary>
        public async IAsyncEnumerable<string> AskStreamAsync(string question, int topK = 5, string role = "user")
        {
            var request = new AskRequest
            {
                Question = question,
                TopK = topK,
                Role = role
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.PostAsync($"{_baseUrl}/ask_stream", content);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new System.IO.StreamReader(stream);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        yield return line;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error streaming from RAG API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get system statistics
        /// </summary>
        public async Task<StatsResponse> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/stats");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StatsResponse>(json, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error getting RAG stats: {ex.Message}", ex);
            }
        }
    }

    // ===== Request/Response Models =====

    public class AskRequest
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("top_k")]
        public int TopK { get; set; } = 5;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";
    }

    public class AskResponse
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("answer")]
        public string Answer { get; set; }

        [JsonPropertyName("sources")]
        public SourceItem[] Sources { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }

    public class SourceItem
    {
        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }
    }

    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("kb_size")]
        public int KbSize { get; set; }

        [JsonPropertyName("log_size")]
        public int LogSize { get; set; }
    }

    public class StatsResponse
    {
        [JsonPropertyName("kb_chunks")]
        public int KbChunks { get; set; }

        [JsonPropertyName("log_chunks")]
        public int LogChunks { get; set; }

        [JsonPropertyName("kb_index_size")]
        public int KbIndexSize { get; set; }

        [JsonPropertyName("log_index_size")]
        public int LogIndexSize { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("llm")]
        public string Llm { get; set; }

        [JsonPropertyName("api_version")]
        public string ApiVersion { get; set; }
    }

    // ===== Usage Example =====
    /*
    // In your .NET controller:
    
    [HttpPost("ask-rag")]
    public async Task<IActionResult> AskRag([FromBody] RagQueryRequest request)
    {
        var client = new RagApiClient("http://localhost:8000");
        
        try
        {
            // Check if server is running
            var health = await client.HealthCheckAsync();
            if (health.Status != "ok")
                return StatusCode(503, "RAG API server not available");

            // Get answer
            var answer = await client.AskJsonAsync(
                question: request.Question,
                topK: request.TopK ?? 5,
                role: User.FindFirst("role")?.Value ?? "user"
            );

            if (!answer.IsSuccess)
                return BadRequest(new { error = answer.Error });

            return Ok(new
            {
                answer = answer.Answer,
                sources = answer.Sources?.Select(s => new
                {
                    s.Score,
                    meta = s.Meta
                }),
                status = "success"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("ask-rag-stream")]
    public async IAsyncEnumerable<string> AskRagStream([FromBody] RagQueryRequest request)
    {
        var client = new RagApiClient("http://localhost:8000");
        await foreach (var chunk in client.AskStreamAsync(
            question: request.Question,
            topK: request.TopK ?? 5,
            role: User.FindFirst("role")?.Value ?? "user"
        ))
        {
            yield return chunk;
        }
    }
    */
}
