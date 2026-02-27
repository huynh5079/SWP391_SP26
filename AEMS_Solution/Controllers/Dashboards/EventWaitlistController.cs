using BusinessLogic.Service.Interfaces;
using BusinessLogic.DTOs.Role.Organizer;
using Microsoft.AspNetCore.Mvc;
using AEMS_Solution.Models.Event;

namespace AEMS_Solution.Controllers.Dashboards;

public class EventWaitlistController : Controller
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

		return View(vm);
	}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToWaitlistRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _waitlistService.AddToWaitlistAsync(dto);
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(RemoveFromWaitlistRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _waitlistService.RemoveFromWaitlistAsync(dto.StudentId, dto.EventId);
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OfferNext(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return BadRequest("eventId is required");

        await _waitlistService.OfferNextAsync(eventId);
        return RedirectToAction("Index", new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(RespondOfferRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _waitlistService.RespondToOfferAsync(dto);
        return RedirectToAction("Index", new { eventId = dto.EventId });
    }
}
