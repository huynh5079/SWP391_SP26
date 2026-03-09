using BusinessLogic.Service;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Api
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ChatbotController : ApiBaseController
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService ?? throw new ArgumentNullException(nameof(chatbotService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gửi câu hỏi và nhận câu trả lời từ chatbot
        /// </summary>
        /// <param name="request">Yêu cầu câu hỏi</param>
        /// <returns>Câu trả lời từ RAG API</returns>
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AskChatbotRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Error("Dữ liệu không hợp lệ", 400);
            }

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Error("Câu hỏi không được để trống", 400);
            }

            try
            {
                var response = await _chatbotService.AskQuestionAsync(request.Question, request.TopK ?? 5);

                if (!response.Success)
                {
                    _logger.LogWarning($"Chatbot response unsuccessful: {response.Message}");
                    return Error(response.Message ?? "Lỗi không xác định", 400);
                }

                return Success(new
                {
                    response.Answer,
                    response.Sources
                }, response.Message ?? "Thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ChatbotController.Ask: {ex.Message}", ex);
                return Error($"Lỗi: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái RAG API server
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var health = await _chatbotService.CheckHealthAsync();
                
                if (!health.IsHealthy)
                {
                    return Error(health.Message ?? "Lỗi không xác định", 503);
                }

                return Success(health, health.Message ?? "Thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ChatbotController.Health: {ex.Message}", ex);
                return Error($"Lỗi: {ex.Message}", 500);
            }
        }
    }

    public class AskChatbotRequest
    {
        public string? Question { get; set; }
        public int? TopK { get; set; } = 5;
    }
}
