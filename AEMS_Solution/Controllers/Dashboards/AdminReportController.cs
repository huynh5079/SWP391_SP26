using AEMS_Solution.Controllers.Common;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Admin")]
    public class AdminReportController : BaseController
    {
        private readonly DataAccess.Repositories.Abstraction.IUnitOfWork _uow;

        public AdminReportController(DataAccess.Repositories.Abstraction.IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // ── 1. Load all events with nav props ─────────────────────────
                var allEvents = (await _uow.Events.GetAllAsync(
                    null,
                    q => q.Include(e => e.Department)
                          .Include(e => e.Tickets)
                              .ThenInclude(t => t.CheckInHistories)
                          .Include(e => e.Feedbacks)
                )).ToList();

                // ── 2. All tickets flat list ───────────────────────────────────
                var allTickets   = allEvents.SelectMany(e => e.Tickets).ToList();
                var allFeedbacks = allEvents.SelectMany(e => e.Feedbacks).ToList();

                // ── 3. KPIs ───────────────────────────────────────────────────
                int totalEvents      = allEvents.Count;
                int totalTickets     = allTickets.Count;
                int totalFeedbacks   = allFeedbacks.Count;
                int totalParticipants = allTickets
                    .Where(t => t.Status == TicketStatusEnum.CheckedIn || t.Status == TicketStatusEnum.Used)
                    .Select(t => t.StudentId)
                    .Distinct()
                    .Count();

                // Budget: load budget proposals
                var allBudgets = await _uow.BudgetProposals.GetAllAsync();
                decimal totalBudgetPlanned = allBudgets.Sum(b => b.PlannedAmount);

                // ── 4. Event Status distribution ─────────────────────────────
                var eventsByStatus = allEvents
                    .GroupBy(e => e.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // ── 5. Event Type distribution ────────────────────────────────
                var eventsByType = allEvents
                    .GroupBy(e => e.Type?.ToString() ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                // ── 6. Monthly trend (last 12 months) ─────────────────────────
                var now         = DateTime.Now;
                var monthLabels    = new List<string>();
                var eventsPerMonth = new List<int>();
                var ticketsPerMonth = new List<int>();

                for (int i = 11; i >= 0; i--)
                {
                    var target = now.AddMonths(-i);
                    var label  = target.ToString("MMM yy");
                    monthLabels.Add(label);

                    int evtCount = allEvents
                        .Count(e => e.CreatedAt.Year == target.Year && e.CreatedAt.Month == target.Month);
                    int tkCount  = allTickets
                        .Count(t => t.CreatedAt.Year == target.Year && t.CreatedAt.Month == target.Month);

                    eventsPerMonth.Add(evtCount);
                    ticketsPerMonth.Add(tkCount);
                }

                // ── 7. Top 10 events by ticket count ─────────────────────────
                var topEvents = allEvents
                    .OrderByDescending(e => e.Tickets.Count)
                    .Take(10)
                    .Select(e => new Models.Admin.EventReportRow
                    {
                        EventId     = e.Id,
                        Title       = e.Title,
                        DeptName    = e.Department?.Name ?? "—",
                        TypeLabel   = e.Type?.ToString() ?? "—",
                        StatusLabel = e.Status.ToString(),
                        TicketCount = e.Tickets.Count,
                        CheckedIn   = e.Tickets.Count(t =>
                            t.Status == TicketStatusEnum.CheckedIn ||
                            t.Status == TicketStatusEnum.Used),
                        Rating      = e.Rating ?? 0,
                        StartDate   = e.StartTime.ToString("dd/MM/yyyy")
                    })
                    .ToList();

                // ── 8. Feedback rating distribution ──────────────────────────
                double avgRating = allFeedbacks.Any(f => f.RatingEvent.HasValue)
                    ? allFeedbacks
                        .Where(f => f.RatingEvent.HasValue)
                        .Average(f => (int)f.RatingEvent!)
                    : 0;

                var ratingDist = new Dictionary<int, int>();
                for (int star = 1; star <= 5; star++)
                {
                    ratingDist[star] = allFeedbacks.Count(f =>
                        f.RatingEvent.HasValue && (int)f.RatingEvent == star);
                }

                // ── 9. Ticket status distribution ────────────────────────────
                var ticketsByStatus = allTickets
                    .GroupBy(t => t.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // ── 10. Department breakdown ──────────────────────────────────
                var deptRows = allEvents
                    .GroupBy(e => e.Department?.Name ?? "Unknown")
                    .Select(g => new Models.Admin.DeptReportRow
                    {
                        DeptName   = g.Key,
                        EventCount = g.Count(),
                        TicketCount = g.Sum(e => e.Tickets.Count)
                    })
                    .OrderByDescending(d => d.EventCount)
                    .ToList();

                // ── Compose ViewModel ─────────────────────────────────────────
                var vm = new Models.Admin.AdminReportViewModel
                {
                    TotalEvents         = totalEvents,
                    TotalTickets        = totalTickets,
                    TotalFeedbacks      = totalFeedbacks,
                    TotalParticipants   = totalParticipants,
                    TotalBudgetPlanned  = totalBudgetPlanned,
                    EventsByStatus      = eventsByStatus,
                    EventsByType        = eventsByType,
                    MonthLabels         = monthLabels,
                    EventsPerMonth      = eventsPerMonth,
                    TicketsPerMonth     = ticketsPerMonth,
                    TopEvents           = topEvents,
                    AverageRating       = Math.Round(avgRating, 2),
                    RatingDist          = ratingDist,
                    TicketsByStatus     = ticketsByStatus,
                    EventsByDepartment  = deptRows,
                    GeneratedAt         = DateTime.Now
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                SetError("Failed to generate report: " + ex.Message);
                return View(new Models.Admin.AdminReportViewModel());
            }
        }
    }
}
