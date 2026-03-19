using BusinessLogic.DTOs.Ticket;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.ValiDateRole.ValidateforOrganizer;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Organizer.CheckIn
{
    public class CheckInService : ICheckInService
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrganizerValidator _validator;

        public CheckInService(IUnitOfWork uow, IOrganizerValidator validator)
        {
            _uow = uow;
            _validator = validator;
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
    }
}
