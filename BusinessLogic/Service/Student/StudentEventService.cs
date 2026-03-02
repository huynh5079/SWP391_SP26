using BusinessLogic.DTOs.Student;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Student
{
    public class StudentEventService : IStudentEventService
    {
        private readonly IUnitOfWork _uow;

        public StudentEventService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ─── Helper: resolve StudentProfile.Id from User.Id ───────────────────
        private async Task<StudentProfile> RequireStudentProfileAsync(string userId)
        {
            var profile = await _uow.StudentProfiles.GetAsync(s => s.UserId == userId);
            if (profile == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ sinh viên. Vui lòng liên hệ quản trị viên.");
            return profile;
        }

        // ─── Map Event entity → browse DTO ────────────────────────────────────
        private static StudentEventBrowseDto MapToBrowseDto(DataAccess.Entities.Event e, int registeredCount, bool isRegistered)
            => new()
            {
                EventId = e.Id,
                Title = e.Title,
                ThumbnailUrl = e.ThumbnailUrl,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location?.Name ?? e.LocationId,
                Status = e.Status,
                MaxCapacity = e.MaxCapacity,
                RegisteredCount = registeredCount,
                TopicName = e.Topic?.Name,
                SemesterName = e.Semester?.Name,
                Mode = e.Mode,
                IsRegistered = isRegistered
            };

        // ─── Dashboard Stats ──────────────────────────────────────────────────
        public async Task<StudentDashboardStatsDto> GetDashboardStatsAsync(string studentId)
        {
            var profile = await RequireStudentProfileAsync(studentId);
            var now = DateTimeHelper.GetVietnamTime();
            var weekEnd = now.AddDays(7);

            // Total registered events (exclude cancelled)
            var tickets = await _uow.Tickets.GetAllAsync(
                t => t.StudentId == profile.Id && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled,
                q => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(q, x => x.Event!)
                      .Include(x => x.Event!.Feedbacks!));

            int totalRegistered = tickets.Count();

            // Upcoming this week
            int upcomingThisWeek = tickets.Count(t => 
                t.Event!.StartTime > now && 
                t.Event!.StartTime <= weekEnd &&
                t.Event!.Status != EventStatusEnum.Cancelled);

            // Pending feedbacks (completed events without student's feedback)
            int pendingFeedbacks = tickets.Count(t => 
                t.Event!.Status == EventStatusEnum.Completed &&
                t.Event!.Feedbacks != null &&
                !t.Event!.Feedbacks.Any(f => f.StudentId == profile.Id && f.DeletedAt == null));

            return new StudentDashboardStatsDto
            {
                TotalRegistered = totalRegistered,
                UpcomingThisWeek = upcomingThisWeek,
                PendingFeedbacks = pendingFeedbacks
            };
        }

        // ─── 1. Browse: all Published/Upcoming/Happening events ───────────────
        public async Task<List<StudentEventBrowseDto>> GetPublishedEventsAsync(
            string studentId,
            string? search = null,
            string? topicId = null,
            string? semesterId = null)
        {
            var profile = await RequireStudentProfileAsync(studentId);

            var events = await _uow.Events.GetAllAsync(
                e => (e.Status == EventStatusEnum.Published ||
                      e.Status == EventStatusEnum.Upcoming  ||
                      e.Status == EventStatusEnum.Happening) &&
                     e.DeletedAt == null,
                q => q.Include(x => x.Location)
                       .Include(x => x.Topic)
                       .Include(x => x.Semester)
                       .Include(x => x.Tickets));

            // In-memory filters
            var filtered = events.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
                filtered = filtered.Where(e => e.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(topicId))
                filtered = filtered.Where(e => e.TopicId == topicId);

            if (!string.IsNullOrWhiteSpace(semesterId))
                filtered = filtered.Where(e => e.SemesterId == semesterId);

            // Get the set of event IDs the student has an active ticket for
            var myTickets = await _uow.Tickets.GetAllAsync(
                t => t.StudentId == profile.Id && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
            var registeredEventIds = myTickets.Select(t => t.EventId).ToHashSet();

            return filtered
                .OrderBy(e => e.StartTime)
                .Select(e =>
                {
                    int count = e.Tickets?.Count(t => t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled) ?? 0;
                    return MapToBrowseDto(e, count, registeredEventIds.Contains(e.Id));
                })
                .ToList();
        }

        // ─── 2. Detail ────────────────────────────────────────────────────────
        public async Task<StudentEventDetailDto> GetEventDetailAsync(string eventId, string studentId)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new InvalidOperationException("Event ID không hợp lệ.");

            var profile = await RequireStudentProfileAsync(studentId);

            var ev = (await _uow.Events.GetAllAsync(
                e => e.Id == eventId,
                q => q.Include(x => x.Location)
                       .Include(x => x.Topic)
                       .Include(x => x.Semester)
                       .Include(x => x.Department)
                       .Include(x => x.Organizer).ThenInclude(o => o!.User)
                       .Include(x => x.Tickets))).FirstOrDefault();

            if (ev == null)
                throw new InvalidOperationException("Event không tồn tại.");

            var now = DateTimeHelper.GetVietnamTime();
            int registeredCount = ev.Tickets?.Count(t => t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled) ?? 0;

            // Check student ticket
            var myTicket = ev.Tickets?.FirstOrDefault(
                t => t.StudentId == profile.Id && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);

            bool isRegistered = myTicket != null;
            bool isFuture = ev.StartTime > now;
            bool isPublic = ev.Status == EventStatusEnum.Published ||
                            ev.Status == EventStatusEnum.Upcoming  ||
                            ev.Status == EventStatusEnum.Happening;

            return new StudentEventDetailDto
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                ThumbnailUrl = ev.ThumbnailUrl,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Location = ev.Location?.Name ?? ev.LocationId,
                MeetingUrl = ev.MeetingUrl,
                Mode = ev.Mode,
                Status = ev.Status,
                MaxCapacity = ev.MaxCapacity,
                RegisteredCount = registeredCount,
                IsDepositRequired = ev.IsDepositRequired,
                DepositAmount = ev.DepositAmount,
                TopicName = ev.Topic?.Name,
                SemesterName = ev.Semester?.Name,
                DepartmentName = ev.Department?.Name,
                OrganizerName = ev.Organizer?.User?.FullName,
                IsRegistered = isRegistered,
                CanRegister = isPublic && isFuture && !isRegistered && registeredCount < ev.MaxCapacity,
                CanCancel = isRegistered && isFuture,
                TicketId = myTicket?.Id
            };
        }

        // ─── 3. Register ──────────────────────────────────────────────────────
        public async Task RegisterForEventAsync(string studentId, string eventId)
        {
            var profile = await RequireStudentProfileAsync(studentId);
            var ev = await _uow.Events.GetAsync(e => e.Id == eventId,
                q => q.Include(x => x.Tickets));

            if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

            var now = DateTimeHelper.GetVietnamTime();
            if (ev.StartTime <= now)
                throw new InvalidOperationException("Không thể đăng ký event đã diễn ra.");

            if (ev.Status != EventStatusEnum.Published &&
                ev.Status != EventStatusEnum.Upcoming)
                throw new InvalidOperationException("Event chưa mở đăng ký.");

            // Check for ANY existing ticket (including cancelled) — DB has a unique index on (EventId, StudentId)
            var existing = await _uow.Tickets.GetAsync(
                t => t.EventId == eventId && t.StudentId == profile.Id);

            if (existing != null)
            {
                // Active ticket → already registered
                if (existing.DeletedAt == null && existing.Status != TicketStatusEnum.Cancelled)
                    throw new InvalidOperationException("Bạn đã đăng ký event này rồi.");

                // Previously cancelled → reactivate instead of inserting new row
                existing.Status = TicketStatusEnum.Registered;
                existing.DeletedAt = null;
                existing.UpdatedAt = DateTimeHelper.GetVietnamTime();
                try
                {
                    await _uow.Tickets.UpdateAsync(existing);
                    await _uow.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    Exception inner = dbEx;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    throw new InvalidOperationException($"Lỗi lưu dữ liệu: {inner.Message}", dbEx);
                }
                return;
            }

            // No existing ticket — create a fresh one
            int activeCount = ev.Tickets?.Count(t => t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled) ?? 0;

            if (activeCount >= ev.MaxCapacity)
            {
                // Stub — waitlist not implemented yet
                await AddToWaitlistAsync(studentId, eventId);
                return;
            }

            // Create Ticket — BaseEntity constructor auto-sets Id, CreatedAt, UpdatedAt
            var ticket = new Ticket
            {
                EventId = eventId,
                StudentId = profile.Id,
                Status = TicketStatusEnum.Registered,
                TicketCode = $"TK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
            };

            try
            {
                await _uow.Tickets.CreateAsync(ticket);
                await _uow.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Unwrap to deepest inner exception for a meaningful message
                Exception inner = dbEx;
                while (inner.InnerException != null) inner = inner.InnerException;
                throw new InvalidOperationException($"Lỗi lưu dữ liệu: {inner.Message}", dbEx);
            }
        }

        // ─── 4. Cancel registration ───────────────────────────────────────────
        public async Task CancelRegistrationAsync(string studentId, string ticketId)
        {
            var profile = await RequireStudentProfileAsync(studentId);

            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == ticketId && t.StudentId == profile.Id && t.DeletedAt == null,
                q => q.Include(x => x.Event));

            if (ticket == null)
                throw new InvalidOperationException("Không tìm thấy vé đăng ký.");

            var now = DateTimeHelper.GetVietnamTime();
            if (ticket.Event.StartTime <= now)
                throw new InvalidOperationException("Không thể hủy đăng ký event đã diễn ra.");

            // Soft-delete (consistent with codebase pattern)
            ticket.Status = TicketStatusEnum.Cancelled;
            ticket.DeletedAt = now;
            await _uow.Tickets.UpdateAsync(ticket);
            await _uow.SaveChangesAsync();
        }

        // ─── 5. My registered events ──────────────────────────────────────────
        public async Task<List<StudentEventBrowseDto>> GetMyRegisteredEventsAsync(string studentId)
        {
            var profile = await RequireStudentProfileAsync(studentId);

            // Fetch active tickets with their events
            var tickets = await _uow.Tickets.GetAllAsync(
                t => t.StudentId == profile.Id && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled,
                q => q.Include(x => x.Event)
                        .ThenInclude(e => e.Location)
                      .Include(x => x.Event)
                        .ThenInclude(e => e.Topic)
                      .Include(x => x.Event)
                        .ThenInclude(e => e.Semester)
                      .Include(x => x.Event)
                        .ThenInclude(e => e.Tickets));

            return tickets
                .Where(t => t.Event.DeletedAt == null)
                .OrderBy(t => t.Event.StartTime)
                .Select(t =>
                {
                    int count = t.Event.Tickets?.Count(tk => tk.DeletedAt == null && tk.Status != TicketStatusEnum.Cancelled) ?? 0;
                    return MapToBrowseDto(t.Event, count, isRegistered: true);
                })
                .ToList();
        }

        // ─── 6. Submit feedback ───────────────────────────────────────────────
        public async Task SubmitFeedbackAsync(string studentId, string eventId, SubmitFeedbackRequestDto dto)
        {
            var profile = await RequireStudentProfileAsync(studentId);
            var ev = await _uow.Events.GetByIdAsync(eventId);
            if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

            var now = DateTimeHelper.GetVietnamTime();
            if (ev.EndTime > now)
                throw new InvalidOperationException("Chỉ được gửi feedback sau khi event kết thúc.");

            // Check student attended
            var ticket = await _uow.Tickets.GetAsync(
                t => t.EventId == eventId && t.StudentId == profile.Id &&
                     t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
            if (ticket == null)
                throw new InvalidOperationException("Bạn chưa đăng ký event này.");

            // Check duplicate feedback
            var existingFeedback = await _uow.Feedbacks.GetAsync(
                f => f.EventId == eventId && f.StudentId == profile.Id && f.DeletedAt == null);
            if (existingFeedback != null)
                throw new InvalidOperationException("Bạn đã gửi feedback cho event này rồi.");

            // BaseEntity constructor auto-sets Id, CreatedAt, UpdatedAt
            var feedback = new Feedback
            {
                EventId = eventId,
                StudentId = profile.Id,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            await _uow.Feedbacks.CreateAsync(feedback);
            await _uow.SaveChangesAsync();
        }

        // ─── Waitlist stub (future phase) ─────────────────────────────────────
        public Task AddToWaitlistAsync(string studentId, string eventId)
        {
            // TODO: Implement waitlist logic in a future phase.
            throw new InvalidOperationException("Event đã đầy. Chức năng đăng ký danh sách chờ sẽ sớm được hỗ trợ.");
        }
    }
}
