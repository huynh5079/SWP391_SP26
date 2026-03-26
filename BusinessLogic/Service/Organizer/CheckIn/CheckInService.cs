using BusinessLogic.DTOs.Ticket;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.ValiDateRole.ValidateforOrganizer;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;
using BusinessLogic.Service.System;
using BusinessLogic.Helper;

namespace BusinessLogic.Service.Organizer.CheckIn
{
    public class CheckInService : ICheckInService
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrganizerValidator _validator;
        private readonly ISignalRNotifier _signalRNotifier;
        private readonly IEmailService _emailService;
        private readonly ISystemErrorLogService _errorLogService;

        public CheckInService(
            IUnitOfWork uow, 
            IOrganizerValidator validator, 
            ISignalRNotifier signalRNotifier, 
            IEmailService emailService,
            ISystemErrorLogService errorLogService)
        {
            _uow = uow;
            _validator = validator;
            _signalRNotifier = signalRNotifier;
            _emailService = emailService;
            _errorLogService = errorLogService;
        }

        public async Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
                {
                    _validator.ValidateCheckInRequest(request);
                }

                var orgProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId);
                if (orgProfile == null)
                {
                    _validator.ValidateOrganizerCanCheckIn(orgProfile);
                }

                // Diagnostic logging
                var payload = request.QrPayload?.Trim();
                var eventId = request.EventId?.Trim();

                // Use IgnoreQueryFilters to catch soft-deleted tickets or wrong-event scans
                var ticket = await _uow.Tickets.GetAsync(
                    t => t.Id.ToLower() == payload.ToLower(),
                    q => q.IgnoreQueryFilters()
                          .Include(t => t.Event)
                          .Include(t => t.Student)
                            .ThenInclude(s => s.User));

                if (ticket == null)
                {
                    // Deeper search for diagnostics
                    var allTicketsForEvent = await _uow.Tickets.CountAsync(t => t.EventId == eventId);
                    return new CheckInResponseDto 
                    { 
                        IsSuccess = false, 
                        Message = $"[DIAG] Không tìm thấy vé! Payload='{payload}' (Len={payload?.Length}). Vé trong Event này: {allTicketsForEvent}. Hãy chắc chắn bạn đã chạy 'Rebuild Solution' để cập nhật code mới nhất." 
                    };
                }

                // Cross-event verification
                if (ticket.EventId != request.EventId)
                {
                    var targetEventTitle = ticket.Event?.Title ?? "sự kiện khác";
                    return new CheckInResponseDto { IsSuccess = false, Message = $"Vé này thuộc về sự kiện: {targetEventTitle}. Vui lòng kiểm tra lại thiết bị quét." };
                }

                if (ticket.DeletedAt != null || ticket.Status == TicketStatusEnum.Cancelled)
                {
                    return new CheckInResponseDto { IsSuccess = false, Message = "Vé này đã bị hủy bỏ hoặc không còn hiệu lực." };
                }

                _validator.ValidateEventOwnership(ticket, orgProfile);
                var now = DateTimeHelper.GetVietnamTime();

                _validator.ValidateNotAlreadyCheckedIn(ticket);
                _validator.ValidateCheckInWindow(ticket, now);

                using var transaction = await _uow.BeginTransactionAsync();
                try
                {
                    ticket.Status = TicketStatusEnum.CheckedIn;
                    ticket.UpdatedAt = now;
                    ticket.CheckInTime = now;

                    await _uow.Tickets.UpdateAsync(ticket);

                    var historyRecord = new CheckInHistory
                    {
                        TicketId = ticket.Id,
                        ScannerId = orgProfile.Id,
                        ScanType = ScanTypeEnum.CheckIn,
                        DeviceName = "Web Scanner"
                    };

                    await _uow.CheckInHistories.CreateAsync(historyRecord);
                    await _uow.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // SignalR Broadcast (async, non-blocking for scanner UI)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var fullNameStr = ticket.Student.User?.FullName ?? ticket.Student.User?.Email ?? "Sinh viên";
                            var avatarUrl = ticket.Student.User?.AvatarUrl;
                            if (string.IsNullOrWhiteSpace(avatarUrl))
                            {
                                var initials = Uri.EscapeDataString(fullNameStr);
                                avatarUrl = $"https://ui-avatars.com/api/?name={initials}&background=random&color=fff";
                            }

                            await _signalRNotifier.SendCheckInNotificationAsync(ticket.EventId, fullNameStr, "Đã check-in thành công!", avatarUrl);
                        }
                        catch { /* SignalR logs internally */ }
                    });

                    return new CheckInResponseDto
                    {
                        IsSuccess = true,
                        Message = "Check-in thành công!",
                        StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                        StudentEmail = ticket.Student.User?.Email
                    };
                }
                catch (Exception updateEx)
                {
                    await transaction.RollbackAsync();
                    throw; // Relaunch to be caught by outer try-catch for logging
                }
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(ex, organizerUserId, "CheckInService.ProcessCheckInAsync");
                return new CheckInResponseDto { IsSuccess = false, Message = $"Có lỗi hệ thống xảy ra: {ex.Message}" };
            }
        }

        public async Task<CheckInResponseDto> ProcessCheckoutAsync(CheckInRequestDto request, string organizerUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
                {
                    _validator.ValidateCheckInRequest(request);
                }

                var orgProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId);
                if (orgProfile == null)
                {
                    _validator.ValidateOrganizerCanCheckIn(orgProfile);
                }

                // Use IgnoreQueryFilters to catch soft-deleted or mismatching tickets during checkout
                var payload = request.QrPayload?.Trim();
                var ticket = await _uow.Tickets.GetAsync(
                    t => t.Id == payload,
                    q => q.IgnoreQueryFilters()
                          .Include(t => t.Event)
                          .Include(t => t.Student)
                            .ThenInclude(s => s.User));

                if (ticket == null)
                {
                    return new CheckInResponseDto { IsSuccess = false, Message = $"Không tìm thấy vé trong DB! QrPayload={request.QrPayload}" };
                }

                if (ticket.EventId != request.EventId)
                {
                    var targetEventTitle = ticket.Event?.Title ?? "sự kiện khác";
                    return new CheckInResponseDto { IsSuccess = false, Message = $"Vé này thuộc về sự kiện: {targetEventTitle}." };
                }

                if (ticket.DeletedAt != null || ticket.Status == TicketStatusEnum.Cancelled)
                {
                    return new CheckInResponseDto { IsSuccess = false, Message = "Vé này đã bị hủy bỏ hoặc không còn hiệu lực." };
                }

                _validator.ValidateEventOwnership(ticket, orgProfile);

                if (ticket.Status != TicketStatusEnum.CheckedIn)
                {
                    return new CheckInResponseDto { IsSuccess = false, Message = "Sinh viên này chưa Check-in hoặc đã Checkout." };
                }

                // Find the latest active check-in record for this ticket
                var checkInRecord = await _uow.CheckInHistories
                    .GetAsync(h => h.TicketId == ticket.Id && h.CheckoutTime == null,
                              q => q.OrderByDescending(h => h.CreatedAt));

                if (checkInRecord == null)
                {
                    return new CheckInResponseDto { IsSuccess = false, Message = "Không tìm thấy thông tin Check-in hợp lệ để Checkout." };
                }

                using var transaction = await _uow.BeginTransactionAsync();
                var now = DateTimeHelper.GetVietnamTime();
                try
                {
                    ticket.Status = TicketStatusEnum.Used;
                    ticket.UpdatedAt = now;
                    await _uow.Tickets.UpdateAsync(ticket);

                    checkInRecord.CheckoutTime = now;
                    checkInRecord.UpdatedAt = now;
                    await _uow.CheckInHistories.UpdateAsync(checkInRecord);

                    await _uow.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // SignalR Notify (async)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var fullNameStr = ticket.Student.User?.FullName ?? ticket.Student.User?.Email ?? "Sinh viên";
                            await _signalRNotifier.SendCheckInNotificationAsync(ticket.EventId, fullNameStr, "Đã checkout thành công!", null);
                        }
                        catch { }
                    });

                    return new CheckInResponseDto
                    {
                        IsSuccess = true,
                        Message = "Checkout thành công!",
                        StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                        StudentEmail = ticket.Student.User?.Email
                    };
                }
                catch (Exception updateEx)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(ex, organizerUserId, "CheckInService.ProcessCheckoutAsync");
                return new CheckInResponseDto { IsSuccess = false, Message = $"Có lỗi hệ thống xảy ra: {ex.Message}" };
            }
        }

        public async Task<List<EventParticipantDto>> GetParticipantsAsync(string eventId)
        {
            var tickets = await _uow.Tickets.GetAllAsync(
                t => t.EventId == eventId,
                q => q.Include(t => t.Student).ThenInclude(s => s.User)
                      .Include(t => t.CheckInHistories)
            );

            return tickets.Select(t =>
            {
                var checkIn = t.CheckInHistories
                    .Where(h => h.ScanType == ScanTypeEnum.CheckIn)
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefault();

                var checkOut = t.CheckInHistories
                    .Where(h => h.ScanType == ScanTypeEnum.CheckOut || h.CheckoutTime != null)
                    .OrderByDescending(h => h.UpdatedAt)
                    .FirstOrDefault();

                DateTime? finalCheckOutTime = null;
                if (checkOut != null)
                {
                    if (checkOut.CheckoutTime != null)
                    {
                        finalCheckOutTime = checkOut.CheckoutTime;
                    }
                    else
                    {
                        finalCheckOutTime = checkOut.UpdatedAt;
                    }
                }

                return new EventParticipantDto
                {
                    TicketId = t.Id,
                    TicketCode = t.TicketCode,
                    StudentId = t.StudentId,
                    FullName = t.Student.User?.FullName ?? "",
                    Email = t.Student.User?.Email ?? "",
                    StudentCode = t.Student.StudentCode ?? "",
                    Status = t.Status,
                    CheckInTime = (checkIn != null) ? checkIn.CreatedAt : (DateTime?)null,
                    CheckOutTime = finalCheckOutTime
                };
            }).ToList();
        }

        public async Task ManualRegisterAsync(string eventId, string userId, string organizerUserId)
        {
            var ev = await _uow.Events.GetAsync(e => e.Id == eventId, q => q.Include(e => e.Location));
            if (ev == null) throw new Exception("Không tìm thấy sự kiện.");

            // Check if already registered
            var student = await _uow.StudentProfiles.GetAsync(s => s.UserId == userId, q => q.Include(s => s.User));
            if (student == null) throw new Exception("Không tìm thấy thông tin sinh viên.");

            // Use IgnoreQueryFilters to find previously cancelled/deleted tickets for reactivation
            var existingTicket = await _uow.Tickets.GetAsync(
                t => t.EventId == eventId && t.StudentId == student.Id,
                q => q.IgnoreQueryFilters());

            // Check capacity before proceeding
            var activeCount = await _uow.Tickets.CountAsync(t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
            if (activeCount >= ev.MaxCapacity)
            {
                throw new Exception("Sự kiện đã hết chỗ.");
            }

            Ticket ticket;
            if (existingTicket != null)
            {
                if (existingTicket.DeletedAt == null && existingTicket.Status != TicketStatusEnum.Cancelled)
                {
                    throw new Exception("Sinh viên này đã có vé cho sự kiện này.");
                }

                // Reactivate existing ticket
                existingTicket.Status = TicketStatusEnum.Registered;
                existingTicket.DeletedAt = null;
                existingTicket.UpdatedAt = DateTimeHelper.GetVietnamTime();
                existingTicket.CheckInTime = null;
                
                await _uow.Tickets.UpdateAsync(existingTicket);
                ticket = existingTicket;
            }
            else
            {
                // Create new ticket if none exists
                ticket = new Ticket
                {
                    EventId = eventId,
                    StudentId = student.Id,
                    Status = TicketStatusEnum.Registered,
                    TicketCode = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };
                await _uow.Tickets.CreateAsync(ticket);
            }

            await _uow.SaveChangesAsync();

            // Send Email
            var qrCode = QRCodeGeneratorHelper.GenerateQRCodeBase64(ticket.Id);
            await _emailService.SendEventRegistrationEmailAsync(
                student.User!.Email,
                student.User.FullName ?? student.User.Email,
                ev.Title,
                ev.StartTime,
                ev.Location?.Name ?? ev.LocationId,
                qrCode
            );
        }

        public async Task CancelTicketAsync(string ticketId, string organizerUserId)
        {
            var ticket = await _uow.Tickets.GetAsync(t => t.Id == ticketId, q => q.Include(t => t.Event).Include(t => t.Student).ThenInclude(s => s.User));
            if (ticket == null) throw new Exception("Không tìm thấy vé.");

            ticket.Status = TicketStatusEnum.Cancelled;
            ticket.DeletedAt = DateTimeHelper.GetVietnamTime(); // Soft delete
            ticket.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _uow.Tickets.UpdateAsync(ticket);
            await _uow.SaveChangesAsync();

            // Send Cancellation Email
            var subject = $"[AEMS] Thông báo hủy vé sự kiện: {ticket.Event.Title}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #fee2e2; border-radius: 8px;'>
                    <h2 style='color: #ef4444;'>THÔNG BÁO HỦY VÉ</h2>
                    <p>Chào <strong>{ticket.Student.User?.FullName ?? ticket.Student.User?.Email}</strong>,</p>
                    <p>Ban tổ chức thông báo vé của bạn cho sự kiện <strong>{ticket.Event.Title}</strong> đã bị hủy.</p>
                    <p>Nếu có thắc mắc, vui lòng liên hệ với Ban tổ chức sự kiện.</p>
                    <hr />
                    <p style='font-size: 12px; color: gray;'>Hệ thống AEMS</p>
                </div>";
            await _emailService.SendAsync(ticket.Student.User!.Email, subject, body);
        }

        public async Task ResendTicketEmailAsync(string ticketId, string organizerUserId)
        {
            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == ticketId, 
                q => q.Include(t => t.Event).ThenInclude(e => e.Location)
                      .Include(t => t.Student).ThenInclude(s => s.User));

            if (ticket == null) throw new Exception("Không tìm thấy vé.");
            if (ticket.Status == TicketStatusEnum.Cancelled || ticket.DeletedAt != null) throw new Exception("Vé này đã bị hủy, không thể gửi lại.");

            var qrCode = QRCodeGeneratorHelper.GenerateQRCodeBase64(ticket.Id);
            await _emailService.SendEventRegistrationEmailAsync(
                ticket.Student.User!.Email,
                ticket.Student.User.FullName ?? ticket.Student.User.Email,
                ticket.Event.Title,
                ticket.Event.StartTime,
                ticket.Event.Location?.Name ?? ticket.Event.LocationId,
                qrCode
            );
        }
    }
}
