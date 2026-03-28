using BusinessLogic.DTOs.Role.Organizer;
using Microsoft.AspNetCore.Mvc;
using BusinessLogic.Service.Event;
using AEMS_Solution.Models.Event;
using AEMS_Solution.Controllers.Common;
using DataAccess.Enum;

namespace AEMS_Solution.Controllers.Features.Event;
public class EventWaitlistController : BaseController
{
    private readonly IEventWaitlistService _waitlistService;

    public EventWaitlistController(IEventWaitlistService waitlistService)
    {
        _waitlistService = waitlistService;
    }

    // GET: /EventWaitlist/Index?eventId=...
    public async Task<IActionResult> Index(string eventId)
    {
		if (string.IsNullOrEmpty(eventId)) return BadRequest("eventId is required");

		var list = await _waitlistService.GetWaitlistByEventAsync(eventId);

		var vm = new EventWaitlistViewModel
		{
			EventId = eventId,
			Items = list
		};

		return View("~/Views/Event/EventWaitList.cshtml",vm);
	}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToWaitlistRequestDto dto)
    {
        try
        {
            await _waitlistService.AddToWaitlistAsync(dto);
            await ExecuteSuccessAsync("Đã thêm vào danh sách chờ.", UserActionType.USER_JOINED_WAITLIST, dto.EventId, TargetType.Event);
        }
        catch (Exception ex)
        {
            await ExecuteErrorAsync(ex, ex.Message);
        }
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(RemoveFromWaitlistRequestDto dto)
    {
        try
        {
            await _waitlistService.RemoveFromWaitlistAsync(dto.StudentId, dto.EventId);
            await ExecuteSuccessAsync("Đã xóa khỏi danh sách chờ.", UserActionType.RemoveMember, dto.EventId, TargetType.Event);
        }
        catch (Exception ex)
        {
            await ExecuteErrorAsync(ex, ex.Message);
        }
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OfferNext(string eventId)
    {
        try
        {
            await _waitlistService.OfferNextAsync(eventId);
            await ExecuteSuccessAsync("Đã gửi lời mời cho người tiếp theo.", UserActionType.Sync, eventId, TargetType.Event);
        }
        catch (Exception ex)
        {
            await ExecuteErrorAsync(ex, ex.Message);
        }
        return RedirectToAction("Index", new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(RespondOfferRequestDto dto)
    {
        try
        {
            await _waitlistService.RespondToOfferAsync(dto);
            await ExecuteSuccessAsync("Đã phản hồi lời mời.", UserActionType.USER_RESPONDED_OFFER, dto.EventId, TargetType.Event);
        }
        catch (Exception ex)
        {
            await ExecuteErrorAsync(ex, ex.Message);
        }
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }
}
