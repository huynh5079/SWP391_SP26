using AEMS_Solution.BaseAction_ValidforController_.Organizer.Event.InterfaceEvent;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.EventAgenda;
using AutoMapper;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Event.EventAgenda
{
	[Authorize(Roles = "Organizer")]
	public class EventAgendaController : BaseController
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IEventAgendaAction _eventAgendaAction;

		public EventAgendaController(IUnitOfWork unitOfWork, IMapper mapper, IEventAgendaAction eventAgendaAction)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_eventAgendaAction = eventAgendaAction;
		}

		[HttpGet]
		public async Task<IActionResult> MyAgenda(string? search = null, string? eventId = null)
		{
			try
			{
				var organizerId = _eventAgendaAction.EnsureOrganizerId(CurrentUserId);

				var events = (await _unitOfWork.Events.GetAllAsync(
					x => x.DeletedAt == null && x.OrganizerId == organizerId))
					.OrderBy(x => x.Title)
					.ToList();

				var agendas = (await _unitOfWork.EventAgenda.GetAllAsync(
					x => x.DeletedAt == null,
					q => q.Include(x => x.Event)))
					.Where(x => x.Event != null && x.Event.DeletedAt == null && x.Event.OrganizerId == organizerId);

				if (!string.IsNullOrWhiteSpace(eventId))
				{
					agendas = agendas.Where(x => x.EventId == eventId);
				}

				if (!string.IsNullOrWhiteSpace(search))
				{
					var keyword = search.Trim();
					agendas = agendas.Where(x =>
						(x.SessionName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
						|| (x.SpeakerInfo?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
						|| (x.Event?.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
				}

				var model = new MyAgendaViewModel
				{
					Search = search,
					SelectedEventId = eventId,
					EventOptions = events.Select(x => new SelectListItem
					{
						Value = x.Id,
						Text = x.Title,
						Selected = x.Id == eventId
					}).ToList(),
					Agendas = _mapper.Map<List<AgendaItemViewModel>>(agendas.OrderBy(x => x.StartTime ?? DateTime.MaxValue).ThenBy(x => x.SessionName).ToList())
				};

				return View("~/Views/Event/Agenda/MyAgenda.cshtml", model);
			}
			catch (UnauthorizedAccessException)
			{
				return RedirectToAction("Login", "Auth");
			}
			catch (InvalidOperationException ex)
			{
				SetError(ex.Message);
				return RedirectToAction("Index", "Organizer");
			}
		}

		[HttpGet]
		public async Task<IActionResult> EditAgenda(string id)
		{
			try
			{
				var organizerId = _eventAgendaAction.EnsureOrganizerId(CurrentUserId);
				var agenda = await _eventAgendaAction.EnsureAgendaOwnershipAsync(organizerId, id);
				var model = _mapper.Map<EditAgendaViewModel>(agenda);
				return View("~/Views/Event/Agenda/EditAgenda.cshtml", model);
			}
			catch (UnauthorizedAccessException)
			{
				return RedirectToAction("Login", "Auth");
			}
			catch (KeyNotFoundException ex)
			{
				SetError(ex.Message);
				return RedirectToAction(nameof(MyAgenda));
			}
			catch (InvalidOperationException ex)
			{
				SetError(ex.Message);
				return RedirectToAction("Index", "Organizer");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditAgenda(EditAgendaViewModel model)
		{
			try
			{
				var organizerId = _eventAgendaAction.EnsureOrganizerId(CurrentUserId);
				var agenda = await _eventAgendaAction.EnsureAgendaOwnershipAsync(organizerId, model.Id);

				if (!string.IsNullOrWhiteSpace(model.EventTitle))
				{
					model.EventTitle = agenda.Event?.Title ?? model.EventTitle;
				}

				if (!ModelState.IsValid)
				{
					return View("~/Views/Event/Agenda/EditAgenda.cshtml", model);
				}

				_eventAgendaAction.EnsureValidAgenda(model);
				_mapper.Map(model, agenda);
				agenda.UpdatedAt = DateTimeHelper.GetVietnamTime();

				await _unitOfWork.EventAgenda.UpdateAsync(agenda);
				await _unitOfWork.SaveChangesAsync();

				await ExecuteSuccessAsync("Cập nhật agenda thành công.", UserActionType.Update, agenda.Id, TargetType.Event);
				return RedirectToAction(nameof(MyAgenda));
			}
			catch (UnauthorizedAccessException)
			{
				return RedirectToAction("Login", "Auth");
			}
			catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
			{
				ModelState.AddModelError(string.Empty, ex.Message);
				return View("~/Views/Event/Agenda/EditAgenda.cshtml", model);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAgenda(string id)
		{
			try
			{
				var organizerId = _eventAgendaAction.EnsureOrganizerId(CurrentUserId);
				var agenda = await _eventAgendaAction.EnsureAgendaOwnershipAsync(organizerId, id);
				agenda.DeletedAt = DateTimeHelper.GetVietnamTime();
				agenda.UpdatedAt = DateTimeHelper.GetVietnamTime();

				await _unitOfWork.EventAgenda.UpdateAsync(agenda);
				await _unitOfWork.SaveChangesAsync();

				await ExecuteSuccessAsync("Xóa agenda thành công.", UserActionType.Delete, agenda.Id, TargetType.Event);
			}
			catch (UnauthorizedAccessException)
			{
				return RedirectToAction("Login", "Auth");
			}
			catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
			{
				await ExecuteErrorAsync(ex, ex.Message);
			}

			return RedirectToAction(nameof(MyAgenda));
		}
	}
}
