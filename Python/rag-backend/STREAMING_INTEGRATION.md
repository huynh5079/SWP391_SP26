# RAG API Streaming Integration Guide

The RAG API now supports **token-by-token streaming responses** via the `/ask-stream` endpoint. This allows real-time response delivery, improving user experience by showing responses as they arrive from the LLM.

## Endpoint Details

### `/ask-stream` (POST)
Streams response tokens one by one using Server-Sent Events (SSE) format with JSON lines.

**Request:**
```json
{
  "question": "Có sự kiện nào cho tân sinh viên không?",
  "top_k": 3,
  "role": "user",
  "session_id": "optional-session-id-for-conversation-memory"
}
```

**Response Stream Format:**
Each line is a separate JSON object:

```json
{"type": "session", "session_id": "uuid"}
{"type": "token", "content": "Có"}
{"type": "token", "content": " "}
{"type": "token", "content": "2"}
{"type": "token", "content": "-"}
{"type": "token", "content": "3"}
{"type": "token", "content": " ..."}
{"type": "sources", "sources": [{"score": "95%", "meta": {...}}, ...]}
{"type": "done"}
```

**Stream Message Types:**
- `session`: First message with session ID for conversation tracking
- `token`: Individual token/character from the LLM response
- `sources`: Retrieved source documents used for RAG
- `error`: Error message (if something goes wrong)
- `done`: Stream completion signal

---

## .NET Integration Example

### Using HttpClient (Recommended)

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class RagStreamingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:8000";

    public RagStreamingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task StreamAnswerAsync(
        string question, 
        string role = "user", 
        string sessionId = null,
        Func<string, Task> onToken = null,
        Func<string, Task> onSessionStart = null,
        Func<Task> onStreamComplete = null)
    {
        var request = new
        {
            question,
            top_k = 3,
            role,
            session_id = sessionId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await _httpClient.PostAsync(
            $"{_baseUrl}/ask-stream",
            content
        );

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                switch (type)
                {
                    case "session":
                        var sessionIdValue = root.GetProperty("session_id").GetString();
                        if (onSessionStart != null)
                            await onSessionStart(sessionIdValue);
                        break;

                    case "token":
                        var tokenContent = root.GetProperty("content").GetString();
                        if (onToken != null)
                            await onToken(tokenContent);
                        break;

                    case "sources":
                        // Handle sources if needed
                        break;

                    case "done":
                        if (onStreamComplete != null)
                            await onStreamComplete();
                        break;

                    case "error":
                        var error = root.GetProperty("content").GetString();
                        Console.WriteLine($"Error: {error}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse: {line}. Error: {ex.Message}");
            }
        }
    }
}
```

### Usage in ASP.NET MVC Controller

```csharp
[HttpPost]
public async Task StreamChat([FromBody] ChatRequest request)
{
    var ragClient = new RagStreamingClient(new HttpClient());
    
    Response.ContentType = "text/event-stream";
    Response.Headers.Add("Cache-Control", "no-cache");
    Response.Headers.Add("Connection", "keep-alive");

    var sb = new StringBuilder();
    
    await ragClient.StreamAnswerAsync(
        question: request.Question,
        role: request.Role ?? "user",
        sessionId: request.SessionId,
        
        onSessionStart: async (sessionId) =>
        {
            await Response.WriteAsync($"data: {JsonConvert.SerializeObject(new { type = "session", sessionId })}\n\n");
            await Response.Body.FlushAsync();
        },
        
        onToken: async (token) =>
        {
            sb.Append(token);
            // Send single token to client
            await Response.WriteAsync($"data: {JsonConvert.SerializeObject(new { type = "token", content = token })}\n\n");
            await Response.Body.FlushAsync();
        },
        
        onStreamComplete: async () =>
        {
            // Stream complete
            await Response.WriteAsync("data: {\"type\": \"done\"}\n\n");
            await Response.Body.FlushAsync();
        }
    );
}
```

### Real-time Chat UI (JavaScript/Fetch API)

```javascript
async function streamChat(question, sessionId = null) {
    const response = await fetch('http://localhost:8000/ask-stream', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            question: question,
            top_k: 3,
            role: 'user',
            session_id: sessionId
        })
    });

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let currentSessionId = null;

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines[lines.length - 1]; // Keep incomplete line

        for (let i = 0; i < lines.length - 1; i++) {
            const line = lines[i].trim();
            if (!line) continue;

            try {
                const msg = JSON.parse(line);

                switch (msg.type) {
                    case 'session':
                        currentSessionId = msg.session_id;
                        console.log('Session:', currentSessionId);
                        break;

                    case 'token':
                        // Append token to chat display
                        document.getElementById('chat-response').textContent += msg.content;
                        break;

                    case 'sources':
                        console.log('Sources:', msg.sources);
                        break;

                    case 'done':
                        console.log('Stream complete');
                        break;

                    case 'error':
                        console.error('Error:', msg.content);
                        break;
                }
            } catch (e) {
                console.error('Failed to parse:', line, e);
            }
        }
    }

    return currentSessionId;
}
```

---

## Testing with cURL

```bash
# Basic streaming request
curl -X POST http://localhost:8000/ask-stream \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Có sự kiện nào cho sinh viên mới?",
    "top_k": 3,
    "role": "user"
  }'

# With Powershell (Windows)
$response = Invoke-WebRequest -Uri "http://localhost:8000/ask-stream" `
  -Method Post `
  -Headers @{"Content-Type" = "application/json"} `
  -Body '{"question":"Có sự kiện nào không?","top_k":3,"role":"user"}'
```

---

## Performance Tips

1. **Token Buffering**: For UI efficiency, consider buffering tokens and flushing every 50-100ms instead of per-token
2. **Cancellation**: Implement cancellation tokens to allow users to stop the stream:
   ```csharp
   var cts = new CancellationTokenSource();
   await ragClient.StreamAnswerAsync(..., cancellationToken: cts.Token);
   ```

3. **Session Reuse**: Pass `session_id` to maintain conversation context across multiple requests

4. **Error Handling**: Always implement proper error handling for network issues:
   ```csharp
   try
   {
       await ragClient.StreamAnswerAsync(...);
   }
   catch (HttpRequestException ex)
   {
       // Handle network errors
   }
   catch (JsonException ex)
   {
       // Handle parsing errors
   }
   ```

---

## Comparison: `/ask` vs `/ask-stream`

| Feature | `/ask` | `/ask-stream` |
|---------|--------|--------------|
| Response | Complete JSON | JSON Lines (streaming) |
| Time to First Token | ~3-8 seconds | <100ms |
| Memory Usage | High (full response) | Low (token-by-token) |
| User Experience | Wait then display | Progressive display |
| Best For | API backends | Real-time UI, Chat |

---

## Troubleshooting

**No tokens appearing?**
- Ensure response media type is `application/json`
- Check that server is running: `http://localhost:8000/health`

**Connection drops?**
- Verify network stability
- Check server logs for errors
- Increase timeout values in client

**Slow streaming?**
- Check Groq API rate limits
- Verify network latency
- Review server resource usage: `http://localhost:8000/stats`

---

## Configuration

Auto-reload interval (affects data freshness):
```bash
RAG_AUTO_RELOAD_SECONDS=60
```

Change in `.env` file before starting server.
