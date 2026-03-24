using System.Security.Claims;
using BusinessLogic.Service.Chat.ChatforUser;
using BusinessLogic.Service.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Chat
{
    [Authorize]
    [Route("chat")]
    public class ChatController : Controller
    {
        private readonly IChatUserService _chatUserService;
        private readonly IChatPresenceTracker _presenceTracker;
        private readonly ISystemErrorLogService _errorLog;

        public ChatController(IChatUserService chatUserService, IChatPresenceTracker presenceTracker, ISystemErrorLogService errorLog)
        {
            _chatUserService = chatUserService;
            _presenceTracker = presenceTracker;
            _errorLog = errorLog;
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> Contacts()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var contacts = (await _chatUserService.GetContactsAsync(currentUserId, currentRole)).ToList();
                foreach (var contact in contacts)
                {
                    contact.IsOnline = _presenceTracker.IsOnline(contact.UserId);
                }
                return Json(new { success = true, data = contacts });
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, currentUserId, $"{nameof(ChatController)}.{nameof(Contacts)}");
                return StatusCode(500, new { success = false, message = "Không tải được danh sách chat." });
            }
        }

        [HttpGet("conversation")]
        public async Task<IActionResult> Conversation([FromQuery] string otherUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(otherUserId))
            {
                return BadRequest(new { success = false, message = "Thiếu otherUserId." });
            }

            try
            {
                var messages = await _chatUserService.GetConversationAsync(currentUserId, currentRole, otherUserId);
                return Json(new { success = true, data = messages });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                await _errorLog.LogErrorAsync(ex, currentUserId, $"{nameof(ChatController)}.{nameof(Conversation)}");
                return Forbid();
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, currentUserId, $"{nameof(ChatController)}.{nameof(Conversation)}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống." });
            }
        }
    }
}
