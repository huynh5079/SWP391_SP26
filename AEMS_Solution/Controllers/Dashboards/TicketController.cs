using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Organizer;
using BusinessLogic.DTOs.Event.Ticket;
using BusinessLogic.Service.Event.Sub_Service.Ticket;
using BusinessLogic.Service.ValidationData.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Organizer")]
    public class TicketController : BaseController
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? eventId, string? studentId)
        {
            var tickets = !string.IsNullOrWhiteSpace(eventId)
                ? await _ticketService.GetTicketsByEventAsync(eventId)
                : !string.IsNullOrWhiteSpace(studentId)
                    ? await _ticketService.GetTicketsByStudentAsync(studentId)
                    : await _ticketService.GetAllTicketsAsync();

            var vm = new TicketIndexViewModel
            {
                EventId = eventId,
                StudentId = studentId,
                Tickets = tickets.Select(x => new TicketListItemVm
                {
                    TicketId = x.Id,
                    EventId = x.EventId,
                    StudentId = x.StudentId,
                    EventName = x.EventName,
                    TicketCode = x.TicketCode,
                    Status = x.Status,
                    CheckInTime = x.CheckInTime,
                    StudentName = x.StudentName
                }).ToList()
            };

            return View("~/Views/Ticket/Index.cshtml", vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Ticket/Create.cshtml", new CreateTicketViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTicketViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Ticket/Create.cshtml", vm);
            }

            try
            {
                await _ticketService.CreateTicketAsync(new CreateTicketDTO
                {
                    EventId = vm.EventId,
                    StudentId = vm.StudentId,
                    TicketCode = vm.TicketCode,
                    Status = vm.Status
                });
                SetSuccess("Tạo ticket thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (TicketValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Ticket/Create.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var ticket = await _ticketService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View("~/Views/Ticket/Edit.cshtml", new UpdateTicketViewModel
            {
                TicketId = ticket.Id,
                EventName = ticket.EventName,
                StudentName = ticket.StudentName,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status,
                CheckInTime = ticket.CheckInTime
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateTicketViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Ticket/Edit.cshtml", vm);
            }

            try
            {
                var updated = await _ticketService.UpdateTicketAsync(vm.TicketId, new UpdateTicketDTO
                {
                    TicketCode = vm.TicketCode,
                    Status = vm.Status,
                    CheckInTime = vm.CheckInTime
                });

                if (!updated)
                {
                    return NotFound();
                }

                SetSuccess("Cập nhật ticket thành công.");
                return RedirectToAction(nameof(Index));
            }
            catch (TicketValidator.BusinessValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return View("~/Views/Ticket/Edit.cshtml", vm);
        }
    }
}
