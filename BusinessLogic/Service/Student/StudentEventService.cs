using BusinessLogic.DTOs.Student;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using System.Data;

using BusinessLogic.Helper;
using BusinessLogic.Service.System;

namespace BusinessLogic.Service.Student
{
    public class StudentEventService : IStudentEventService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly ISystemErrorLogService _errorLogService;
        private readonly INotificationService _notificationService;

        public StudentEventService(IUnitOfWork uow, IEmailService emailService, ISystemErrorLogService errorLogService, INotificationService notificationService)
        {
            _uow = uow;
            _emailService = emailService;
            _errorLogService = errorLogService;
            _notificationService = notificationService;
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
                ThumbnailUrl = e.ThumbnailUrl?.Split('|')[0],
                ImageUrls = string.IsNullOrEmpty(e.ThumbnailUrl) ? new List<string>() : e.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location != null ? $"{e.Location.Name} - {e.Location.Address}" : e.LocationId,
                Status = e.Status,
                MaxCapacity = e.MaxCapacity,
                RegisteredCount = registeredCount,
                TopicName = e.Topic?.Name,
                SemesterName = e.Semester?.Name.ToString(),
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
                       .Include(x => x.Tickets)
                       .Include(x => x.EventDocuments)
               // Include agendas and speaker profiles/users for richer agenda info
               .Include(x => x.EventAgenda).ThenInclude(ag => ag.StudentSpeaker).ThenInclude(s => s.User)
               .Include(x => x.EventAgenda).ThenInclude(ag => ag.StaffSpeaker).ThenInclude(s => s.User)
               // Include teams and members for speaker/BTC panel
               .Include(x => x.EventTeams).ThenInclude(et => et.TeamMembers).ThenInclude(tm => tm.Student).ThenInclude(s => s!.User)
               .Include(x => x.EventTeams).ThenInclude(et => et.TeamMembers).ThenInclude(tm => tm.Staff).ThenInclude(s => s!.User)
)).FirstOrDefault();

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

            var feedbackStatus = now < ev.StartTime
                ? FeedbackStatusEnum.BeforeEvent
                : now <= ev.EndTime
                    ? FeedbackStatusEnum.DuringEvent
                    : FeedbackStatusEnum.AfterEvent;

            var existingFeedback = await _uow.Feedbacks.GetAsync(
                f => f.EventId == eventId && f.StudentId == profile.Id && f.DeletedAt == null);
            //waitlist status
            var waitlistEntry = await _uow.EventWaitlist.GetAsync(
                w => w.EventId == eventId && w.StudentId == profile.Id);

            bool isInWaitlist = waitlistEntry != null &&
                (waitlistEntry.Status == DataAccess.Enum.EventWaitlistStatusEnum.Waiting ||
                 waitlistEntry.Status == DataAccess.Enum.EventWaitlistStatusEnum.Offered);

            // agendas details
            var agendas = ev.EventAgenda?
    .Where(a => a.DeletedAt == null)
    .OrderBy(a => a.StartTime)
    .Select(a => new EventAgendaItemDto
    {
        SessionName = a.SessionName,
        Description = a.Description,
        SpeakerName = a.StudentSpeaker?.User?.FullName ?? a.StaffSpeaker?.User?.FullName ?? a.SpeakerInfo,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Location = a.Location
    })
    .ToList();

            var documents = ev.EventDocuments?.Select(d => new EventDocumentDto
            {
                Name = d.Name,
                Url = d.Url,
                Type = d.Type
            }).ToList();

            // ─── Participation role for the current student ────────────────────
            bool isTeamMember = ev.EventTeams?.Any(et =>
                et.DeletedAt == null &&
                et.TeamMembers.Any(tm => tm.StudentId == profile.Id)) ?? false;

            bool isSpeaker = ev.EventAgenda?.Any(a =>
                a.StudentSpeakerId == profile.Id && a.DeletedAt == null) ?? false;

            string? participationRole = null;
            if (isTeamMember)       participationRole = "Ban tổ chức";
            else if (isSpeaker)     participationRole = "Diễn giả";
            else if (isRegistered)  participationRole = "Khách tham dự";

            // ─── Read-only teams for speaker/BTC view ─────────────────────────
            List<EventTeamReadOnlyDto>? teams = null;
            if (participationRole == "Ban tổ chức" || participationRole == "Diễn giả")
            {
                teams = ev.EventTeams?
                    .Where(et => et.DeletedAt == null)
                    .Select(et => new EventTeamReadOnlyDto
                    {
                        TeamName = et.TeamName ?? "",
                        Description = et.Description,
                        Members = et.TeamMembers.Select(tm => new TeamMemberReadOnlyDto
                        {
                            MemberName = tm.Student?.User?.FullName
                                      ?? tm.Staff?.User?.FullName
                                      ?? "(Chưa rõ)",
                            RoleName = tm.Role?.ToString() ?? "",
                            UserId    = tm.Student?.UserId ?? tm.Staff?.UserId
                        }).ToList()
                    }).ToList();
            }

            return new StudentEventDetailDto
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                ThumbnailUrl = ev.ThumbnailUrl?.Split('|')[0],
                ImageUrls = string.IsNullOrEmpty(ev.ThumbnailUrl) ? new List<string>() : ev.ThumbnailUrl.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Location = ev.Location != null ? $"{ev.Location.Name} - {ev.Location.Address}" : ev.LocationId,
                MeetingUrl = ev.MeetingUrl,
                Mode = ev.Mode,
                Status = ev.Status,
                MaxCapacity = ev.MaxCapacity,
                RegisteredCount = registeredCount,
                IsDepositRequired = ev.IsDepositRequired,
                DepositAmount = ev.DepositAmount,
                TopicName = ev.Topic?.Name,
                SemesterName = ev.Semester?.Name.ToString(),
                DepartmentName = ev.Department?.Name,
                OrganizerName = ev.Organizer?.User?.FullName,
                OrganizerUserId = ev.Organizer?.UserId,
                OrganizerAvatarUrl = ev.Organizer?.User?.AvatarUrl,
                OrganizerPosition = ev.Organizer?.Position,
                IsRegistered = isRegistered,
                CanRegister = isPublic && isFuture && !isRegistered && registeredCount < ev.MaxCapacity && !isInWaitlist,
                CanCancel = isRegistered && isFuture,
                TicketId = myTicket?.Id,
                //IsFull = registeredCount >= ev.MaxCapacity,
                IsInWaitlist = isInWaitlist,
                WaitlistPosition = waitlistEntry?.Position,
                WaitlistStatus = waitlistEntry?.Status,        // ✅ thêm
                WaitlistStudentProfileId = waitlistEntry?.StudentId,
                FeedbackStatus = feedbackStatus,
                HasSubmittedFeedback = existingFeedback != null,
                CurrentFeedbackRating = existingFeedback != null && (int)existingFeedback.RatingEvent >= 1
                    ? (int?)((int)existingFeedback.RatingEvent)
                    : null,
                CurrentFeedbackComment = existingFeedback?.Comment,
                Agendas = agendas,
                Documents = documents,
                ParticipationRole = participationRole,
                Teams = teams
            };
        }

        // ─── 3. Register ──────────────────────────────────────────────────────
        public async Task RegisterForEventAsync(string studentId, string eventId)
        {
            var profile = await RequireStudentProfileAsync(studentId);
            Ticket? activeTicket = null;
            Ticket? reactivatedTicket = null;
            global::DataAccess.Entities.Event? ev;

            using var newTrans = await _uow.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                ev = await _uow.Events.GetAsync(e => e.Id == eventId);
                if (ev == null) throw new InvalidOperationException("Event không tồn tại.");

                var now = DateTimeHelper.GetVietnamTime();
           // if (now < ev.StartTime && dto.Rating.HasValue)
             //   throw new InvalidOperationException("Chỉ được đánh giá sao trong hoặc sau khi sự kiện bắt đầu.");
                if (ev.StartTime <= now)
                    throw new InvalidOperationException("Không thể đăng ký event đã diễn ra.");

                if (ev.Status != EventStatusEnum.Published &&
                    ev.Status != EventStatusEnum.Upcoming)
                    throw new InvalidOperationException("Event chưa mở đăng ký.");

                // Use IgnoreQueryFilters to retrieve even soft-deleted (Cancelled) tickets so we can reactivate them!
                var existing = await _uow.Tickets.GetAsync(
                    t => t.EventId == eventId && t.StudentId == profile.Id,
                    q => q.IgnoreQueryFilters());

                if (existing != null)
                {
                    if (existing.DeletedAt == null && existing.Status != TicketStatusEnum.Cancelled)
                        throw new InvalidOperationException("Bạn đã đăng ký event này rồi.");

                    var activeCount = await _uow.Tickets.CountAsync(
                        t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
                    if (activeCount >= ev.MaxCapacity)
                    {
                        await newTrans.RollbackAsync();
                        await AddToWaitlistAsync(studentId, eventId);
                        return;
                    }

                    existing.Status = TicketStatusEnum.Registered;
                    existing.DeletedAt = null;
                    existing.CheckInTime = null;
                    await _uow.Tickets.UpdateAsync(existing);
                    await _uow.SaveChangesAsync();
                    await newTrans.CommitAsync();
                    reactivatedTicket = existing;
                }
                else
                {
                    var activeCount = await _uow.Tickets.CountAsync(
                        t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);

                    if (activeCount >= ev.MaxCapacity)
                    {
                        await newTrans.RollbackAsync();
                        await AddToWaitlistAsync(studentId, eventId);
                        return;
                    }

                    activeTicket = new Ticket
                    {
                        EventId = eventId,
                        StudentId = profile.Id,
                        Status = TicketStatusEnum.Registered,
                        TicketCode = $"TK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                    };

                    await _uow.Tickets.CreateAsync(activeTicket);
                    await _uow.SaveChangesAsync();
                    await newTrans.CommitAsync();
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                await newTrans.RollbackAsync();
                Exception inner = dbEx;
                while (inner.InnerException != null) inner = inner.InnerException;
                
                if (inner.Message.Contains("UIX_Ticket_Event_Student") || inner.Message.Contains("duplicate key"))
                {
                    throw new InvalidOperationException("Bạn đã đăng ký event này rồi.");
                }

                throw new InvalidOperationException($"Lỗi lưu dữ liệu: {inner.Message}", dbEx);
            }

            var savedTicket = reactivatedTicket ?? activeTicket;
            if (savedTicket == null)
            {
                return;
            }

            ev = await _uow.Events.GetAsync(e => e.Id == eventId, q => q.Include(x => x.Location));
            if (ev == null)
            {
                return;
            }

            try
            {
                string qrPayload = savedTicket.Id;
                string qrCodeBase64 = QRCodeGeneratorHelper.GenerateQRCodeBase64(qrPayload);

                var user = await _uow.Users.GetAsync(u => u.Id == profile.UserId);
                if (user != null)
                {
                    string locationName = ev.Location?.Name ?? ev.LocationId ?? "N/A";

                    await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
                    {
                        ReceiverId = user.Id,
                        Title = "Đăng ký thành công",
                        Message = reactivatedTicket == null
                            ? $"Bạn đã đăng ký thành công sự kiện '{ev.Title}'. Vui lòng kiểm tra email để nhận mã QR check-in."
                            : $"Bạn đã đăng ký lại thành công sự kiện '{ev.Title}'. Vui lòng kiểm tra email để nhận mã QR.",
                        Type = DataAccess.Enum.NotificationType.TicketCreated,
                        RelatedEntityId = savedTicket.Id
                    });

                    await _emailService.SendEventRegistrationEmailAsync(
                        user.Email,
                        user.FullName ?? user.Email ?? "Sinh viên",
                        ev.Title,
                        ev.StartTime,
                        locationName,
                        qrCodeBase64
                    );
                }
            }
            catch (Exception emailEx)
            {
                await _errorLogService.LogErrorAsync(
                    emailEx,
                    profile.UserId,
                    reactivatedTicket == null
                        ? "StudentEventService.RegisterForEventAsync (New Ticket)"
                        : "StudentEventService.RegisterForEventAsync (Reactivate)");
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

            // ✅ Offer người tiếp theo trong waitlist
            try
            {
                var nextInLine = (await _uow.EventWaitlist.GetAllAsync(
                    w => w.EventId == ticket.EventId &&
                         w.Status == DataAccess.Enum.EventWaitlistStatusEnum.Waiting,
                    q => q.Include(x => x.Student).ThenInclude(s => s.User)
                           .OrderBy(x => x.Position))).FirstOrDefault();

                if (nextInLine != null)
                {
                    nextInLine.Status = DataAccess.Enum.EventWaitlistStatusEnum.Offered;
                    nextInLine.OfferedAt = now;
                    nextInLine.IsNotified = true;
                    nextInLine.UpdatedAt = now;
                    await _uow.EventWaitlist.UpdateAsync(nextInLine);
                    await _uow.SaveChangesAsync();

                    var offeredUser = nextInLine.Student?.User;
                    if (offeredUser != null)
                    {
                        // In-app notification
                        await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
                        {
                            ReceiverId = offeredUser.Id,
                            Title = "Có chỗ trống cho bạn!",
                            Message = $"Có một chỗ trống trong sự kiện '{ticket.Event.Title}'. Hãy vào đăng ký ngay!",
                            Type = DataAccess.Enum.NotificationType.TicketCreated,
                            RelatedEntityId = ticket.EventId
                        });

                        // Send email (new) — same style as NotifyOfferedStudentAsync
                        try
                        {
                            await _emailService.SendAsync(
                                offeredUser.Email,
                                $"[AEMS] Có chỗ trống – {ticket.Event.Title}",
                                $@"<p>Xin chào <strong>{offeredUser.FullName}</strong>,</p>
                                   <p>Một chỗ trống vừa mở trong sự kiện <strong>{ticket.Event.Title}</strong>
                                      lúc <strong>{ticket.Event.StartTime:HH:mm, dd/MM/yyyy}</strong>.</p>
                                   <p>Hãy đăng ký ngay trước khi hết chỗ!</p>"
                            );
                        }
                        catch (Exception emailEx)
                        {
                            await _errorLogService.LogErrorAsync(emailEx, profile.UserId,
                                "StudentEventService.CancelRegistrationAsync (NotifyOfferedEmail)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(ex, profile.UserId,
                    "StudentEventService.CancelRegistrationAsync (OfferWaitlist)");
            }

            // Send Generic in-app notification
            await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
            {
                ReceiverId = profile.UserId,
                Title = "Hủy đăng ký",
                Message = $"Bạn đã hủy đăng ký sự kiện '{ticket.Event.Title}'.",
                Type = DataAccess.Enum.NotificationType.EventCancel,
                RelatedEntityId = ticket.EventId
            });
        }

        // ─── 5. My participations ─────────────────────────────────────────────
        public async Task<List<StudentEventBrowseDto>> GetMyParticipationsAsync(string studentId)
        {
            var profile = await RequireStudentProfileAsync(studentId);

            // Fetch active tickets or where student is team member or speaker
            var events = await _uow.Events.GetAllAsync(
                e => e.DeletedAt == null && (
                     e.Tickets.Any(t => (t.StudentId == profile.Id || (t.Student != null && t.Student.UserId == studentId)) && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled) ||
                     e.EventTeams.Any(et => et.DeletedAt == null && et.TeamMembers.Any(tm => tm.StudentId == profile.Id || (tm.Student != null && tm.Student.UserId == studentId))) ||
                     e.EventAgenda.Any(a => (a.StudentSpeakerId == profile.Id || (a.StudentSpeaker != null && a.StudentSpeaker.UserId == studentId)) && a.DeletedAt == null)),
                q => q.Include(x => x.Location)
                      .Include(x => x.Topic)
                      .Include(x => x.Semester)
                      .Include(x => x.Tickets).ThenInclude(t => t.Student)
                      .Include(x => x.EventTeams).ThenInclude(et => et.TeamMembers).ThenInclude(tm => tm.Student)
                      .Include(x => x.EventAgenda).ThenInclude(a => a.StudentSpeaker));

            return events
                .OrderBy(e => e.StartTime)
                .Select(e =>
                {
                    int count = e.Tickets?.Count(tk => tk.DeletedAt == null && tk.Status != TicketStatusEnum.Cancelled) ?? 0;
                    
                    // We mark isRegistered as true if they have an active ticket
                    bool hasActiveTicket = e.Tickets?.Any(t => (t.StudentId == profile.Id || (t.Student != null && t.Student.UserId == studentId)) && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled) ?? false;
                    
                    // Determine Role
                    string role = "";
                    if (e.EventTeams.Any(et => et.TeamMembers.Any(tm => tm.StudentId == profile.Id || (tm.Student != null && tm.Student.UserId == studentId))))
                    {
                        role = "Ban tổ chức";
                    }
                    else if (e.EventAgenda.Any(a => (a.StudentSpeakerId == profile.Id || (a.StudentSpeaker != null && a.StudentSpeaker.UserId == studentId)) && a.DeletedAt == null))
                    {
                        role = "Diễn giả";
                    }
                    else if (hasActiveTicket)
                    {
                        role = "Khách tham dự";
                    }

                    var dto = MapToBrowseDto(e, count, isRegistered: hasActiveTicket);
                    dto.ParticipationRole = role;
                    return dto;
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
            var canRate = now >= ev.StartTime;
            if (!canRate && dto.Rating.HasValue)
                throw new InvalidOperationException("Chỉ được đánh giá sao trong hoặc sau khi sự kiện bắt đầu.");

            // Check student attended
            var ticket = await _uow.Tickets.GetAsync(
                t => t.EventId == eventId && t.StudentId == profile.Id &&
                     t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
            if (ticket == null)
                throw new InvalidOperationException("Bạn chưa đăng ký event này.");

            // Check duplicate feedback
            var existingFeedback = await _uow.Feedbacks.GetAsync(
                f => f.EventId == eventId && f.StudentId == profile.Id && f.DeletedAt == null);

            var status = now < ev.StartTime
                ? FeedbackStatusEnum.BeforeEvent
                : now <= ev.EndTime
                    ? FeedbackStatusEnum.DuringEvent
                    : FeedbackStatusEnum.AfterEvent;

            if (existingFeedback == null)
            {
                var feedback = new Feedback
                {
                    EventId = eventId,
                    StudentId = profile.Id,
                    Comment = dto.Comment,
                    Status = status,
                    RatingEvent = dto.Rating.HasValue
                        ? (FeedBackRatingsEnum)dto.Rating.Value
                        : (FeedBackRatingsEnum)0
                };

                await _uow.Feedbacks.CreateAsync(feedback);
            }
            else
            {
                existingFeedback.Comment = dto.Comment;
                if (dto.Rating.HasValue)
                {
                    existingFeedback.RatingEvent = (FeedBackRatingsEnum)dto.Rating.Value;
                }
                existingFeedback.Status = status;
                existingFeedback.UpdatedAt = now;

                await _uow.Feedbacks.UpdateAsync(existingFeedback);
            }

            await _uow.SaveChangesAsync();

            // Send notification to Organizer
            var organizerProfile = await _uow.StaffProfiles.GetAsync(sp => sp.Id == ev.OrganizerId);
            if (organizerProfile?.UserId != null)
            {
                await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
                {
                    ReceiverId = organizerProfile.UserId,
                    Title = "Có đánh giá mới",
                    Message = $"Một sinh viên vừa gửi đánh giá cho sự kiện '{ev.Title}'. Rating: {dto.Rating}/5",
                    Type = DataAccess.Enum.NotificationType.EventFeedback,
                    RelatedEntityId = ev.Id
                });
            }
        }

        public async Task<List<StudentEventFeedbackItemDto>> GetEventFeedbacksAsync(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return new List<StudentEventFeedbackItemDto>();

            var feedbacks = await _uow.Feedbacks.GetAllAsync(
                f => f.EventId == eventId && f.DeletedAt == null,
                q => q.Include(f => f.Student).ThenInclude(s => s!.User));

            return feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new StudentEventFeedbackItemDto
                {
                    EventId = f.EventId ?? string.Empty,
                    StudentCode = f.Student?.StudentCode,
                    StudentName = f.Student?.User?.FullName,
                    Rating = (int)f.RatingEvent,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToList();
        }

        // ─── Waitlist ─────────────────────────────────────────────────────────
        public async Task AddToWaitlistAsync(string studentId, string eventId)
        {
            var profile = await RequireStudentProfileAsync(studentId);

            var existing = await _uow.EventWaitlist.GetAsync(
                w => w.EventId == eventId && w.StudentId == profile.Id && w.DeletedAt == null);

            if (existing != null)
                throw new InvalidOperationException("Bạn đã có trong danh sách chờ của sự kiện này.");

            var count = await _uow.EventWaitlist.CountAsync(w => w.EventId == eventId && w.DeletedAt == null);
            var now = DateTimeHelper.GetVietnamTime();

            var entry = new DataAccess.Entities.EventWaitlist
            {
                Id = Guid.NewGuid().ToString(),
                EventId = eventId,
                StudentId = profile.Id,
                JoinedAt = now,
                IsNotified = false,
                Status = DataAccess.Enum.EventWaitlistStatusEnum.Waiting,
                Position = count + 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _uow.EventWaitlist.CreateAsync(entry);
            await _uow.SaveChangesAsync();

            // Thông báo cho sinh viên
            var user = await _uow.Users.GetAsync(u => u.Id == studentId);
            if (user != null)
            {
                await _notificationService.SendNotificationAsync(new BusinessLogic.DTOs.SendNotificationRequest
                {
                    ReceiverId = user.Id,
                    Title = "Đã vào danh sách chờ",
                    Message = $"Sự kiện đã đầy. Bạn đang ở vị trí #{entry.Position} trong danh sách chờ.",
                    Type = DataAccess.Enum.NotificationType.TicketCreated,
                    RelatedEntityId = eventId
                });
            }
        }
    }
}
