using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace AEMS_Solution.Models.Admin
{
    /// <summary>
    /// Full ViewModel for the Admin Reports page.
    /// All data is sourced from real DB queries — zero fake data.
    /// </summary>
    public class AdminReportViewModel
    {
        // ── Overall KPIs ──────────────────────────────────────────────────────
        public int TotalEvents          { get; set; }
        public int TotalTickets         { get; set; }
        public int TotalFeedbacks       { get; set; }
        public int TotalParticipants    { get; set; }   // unique students who attended
        public decimal TotalBudgetPlanned { get; set; }

        // ── Event Status Breakdown ────────────────────────────────────────────
        public Dictionary<string, int> EventsByStatus { get; set; } = new();

        // ── Event Type Breakdown ──────────────────────────────────────────────
        public Dictionary<string, int> EventsByType { get; set; } = new();

        // ── Events Per Month (12 months rolling) ─────────────────────────────
        public List<string> MonthLabels    { get; set; } = new();
        public List<int>    EventsPerMonth { get; set; } = new();
        public List<int>    TicketsPerMonth { get; set; } = new();

        // ── Top events by participant count ──────────────────────────────────
        public List<EventReportRow> TopEvents { get; set; } = new();

        // ── Feedback summary ─────────────────────────────────────────────────
        public double AverageRating           { get; set; }
        public Dictionary<int, int> RatingDist { get; set; } = new();  // star → count

        // ── Ticket status breakdown ───────────────────────────────────────────
        public Dictionary<string, int> TicketsByStatus { get; set; } = new();

        // ── Department breakdown ──────────────────────────────────────────────
        public List<DeptReportRow> EventsByDepartment { get; set; } = new();

        // Report generated timestamp
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    public class EventReportRow
    {
        public string EventId       { get; set; } = "";
        public string Title         { get; set; } = "";
        public string DeptName      { get; set; } = "";
        public string TypeLabel     { get; set; } = "";
        public string StatusLabel   { get; set; } = "";
        public int    TicketCount   { get; set; }
        public int    CheckedIn     { get; set; }
        public double Rating        { get; set; }
        public string StartDate     { get; set; } = "";
    }

    public class DeptReportRow
    {
        public string DeptName      { get; set; } = "";
        public int    EventCount    { get; set; }
        public int    TicketCount   { get; set; }
    }
}
