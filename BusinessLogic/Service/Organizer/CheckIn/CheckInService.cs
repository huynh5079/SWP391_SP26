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

        public CheckInService(IUnitOfWork uow, IOrganizerValidator validator, ISignalRNotifier signalRNotifier, IEmailService emailService)
        {
            _uow = uow;
            _validator = validator;
            _signalRNotifier = signalRNotifier;
            _emailService = emailService;
        }

        public async Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId)
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

            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == request.QrPayload && t.EventId == request.EventId,
                q => q.Include(t => t.Event)
                      .Include(t => t.Student)
                        .ThenInclude(s => s.User));

            if (ticket == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = $"Không tìm thấy vé trong DB! QrPayload={request.QrPayload}, EventId={request.EventId}" };
            }
            if (ticket.DeletedAt != null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Vé đã bị xóa khỏi hệ thống." };
            }
            if (ticket.Status == TicketStatusEnum.Cancelled)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Vé này đã bị hủy bỏ." };
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

                await _uow.Tickets.UpdateAsync(ticket);

                var historyRecord = new CheckInHistory
                {
                    TicketId = ticket.Id,
                    ScannerId = orgProfile.Id,
                    ScanType = ScanTypeEnum.CheckIn,
                    DeviceName = "Mobile Web Scanner",
                    Location = ticket.Event.Location?.Name
                };

                await _uow.CheckInHistories.CreateAsync(historyRecord);

                await _uow.SaveChangesAsync();
                await transaction.CommitAsync();

                // SignalR Broadcast for Live Display
                try
                {
                    var fullNameStr = ticket.Student.User?.FullName ?? ticket.Student.User?.Email ?? "Sinh viên";
                    var avatarUrl = ticket.Student.User?.AvatarUrl;
                    if (string.IsNullOrWhiteSpace(avatarUrl)) 
                    {
                        var initials = !string.IsNullOrWhiteSpace(fullNameStr) ? Uri.EscapeDataString(fullNameStr) : "S";
                        avatarUrl = $"https://ui-avatars.com/api/?name={initials}&background=random&color=fff";
                    }

                    await _signalRNotifier.SendCheckInNotificationAsync(
                        ticket.EventId,
                        fullNameStr,
                        "Chào mừng đến với sự kiện 🫶",
                        avatarUrl
                    );
                }
                catch { /* Fail silently if SignalR fails */ }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                try
                {
                    var errorLog = new SystemErrorLog
                    {
                        UserId = organizerUserId,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        Source = "CheckInService.ProcessCheckInAsync"
                    };
                    await _uow.SystemErrorLogs.CreateAsync(errorLog);
                    await _uow.SaveChangesAsync();
                }
                catch { }

                return new CheckInResponseDto
                {
                    IsSuccess = false,
                    Message = "Lỗi khi lưu dữ liệu Check-in."
                };
            }
            return new CheckInResponseDto
            {
                IsSuccess = true,
                Message = "Check-in thành công!",
                StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                StudentEmail = ticket.Student.User?.Email
            };
        }

        public async Task<CheckInResponseDto> ProcessCheckoutAsync(CheckInRequestDto request, string organizerUserId)
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

            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == request.QrPayload && t.EventId == request.EventId,
                q => q.Include(t => t.Event).Include(t => t.Student).ThenInclude(s => s.User));

            if (ticket == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = $"Không tìm thấy vé trong DB! QrPayload={request.QrPayload}, EventId={request.EventId}" };
            }
            if (ticket.DeletedAt != null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Vé đã bị xóa khỏi hệ thống." };
            }
            if (ticket.Status == TicketStatusEnum.Cancelled)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Vé này đã bị hủy bỏ." };
            }

            _validator.ValidateEventOwnership(ticket, orgProfile);

            if (ticket.Status != TicketStatusEnum.CheckedIn)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Sinh viên này chưa Check-in hoặc đã Checkout." };
            }

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
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                try
                {
                    var errorLog = new SystemErrorLog
                    {
                        UserId = organizerUserId,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        Source = "CheckInService.ProcessCheckoutAsync"
                    };
                    await _uow.SystemErrorLogs.CreateAsync(errorLog);
                    await _uow.SaveChangesAsync();
                }
                catch { }

                return new CheckInResponseDto
                {
                    IsSuccess = false,
                    Message = "Lỗi khi lưu dữ liệu Checkout."
                };
            }

            return new CheckInResponseDto
            {
                IsSuccess = true,
                Message = "Checkout thành công!",
                StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                StudentEmail = ticket.Student.User?.Email
            };
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

            var existingTicket = await _uow.Tickets.GetAsync(t => t.EventId == eventId && t.StudentId == student.Id && t.DeletedAt == null);
            if (existingTicket != null) throw new Exception("Sinh viên này đã có vé cho sự kiện này.");

            // Check capacity
            var registeredCount = await _uow.Tickets.CountAsync(t => t.EventId == eventId && t.DeletedAt == null && t.Status != TicketStatusEnum.Cancelled);
            if (registeredCount >= ev.MaxCapacity)
            {
                throw new Exception("Sự kiện đã hết chỗ.");
            }

            var ticket = new Ticket
            {
                EventId = eventId,
                StudentId = student.Id,
                Status = TicketStatusEnum.Registered,
                TicketCode = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _uow.Tickets.CreateAsync(ticket);
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
