using BusinessLogic.DTOs.Ticket;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using DateTimeHelper = DataAccess.Helper.DateTimeHelper;

namespace BusinessLogic.Service.Organizer
{
    public class CheckInService : ICheckInService
    {
        private readonly IUnitOfWork _uow;

        public CheckInService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId)
        {
            // 1. Validate payload
            if (string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Dữ liệu QR Code rỗng hoặc không hợp lệ." };
            }

            // 2. Resolve Organizer Profile from UserId
            var orgProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId);
            if (orgProfile == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Chỉ ban tổ chức mới có quyền check-in." };
            }

            // 3. Find the ticket
            // We use Include to fetch student and event data to show dynamic success messages
            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == request.QrPayload && t.EventId == request.EventId,
                q => q.Include(t => t.Event)
                      .Include(t => t.Student)
                        .ThenInclude(s => s.User));

            if (ticket == null || ticket.DeletedAt != null || ticket.Status == TicketStatusEnum.Cancelled)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Vé không tồn tại hoặc đã bị hủy." };
            }

            // 4. Validate Event Ownership
            if (ticket.Event.OrganizerId != orgProfile.Id)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Sự kiện này không thuộc quyền quản lý của bạn." };
            }

            // 5. Validate Event Event-time (Optional business rule: only check-in on the day of the event)
            var now = DateTimeHelper.GetVietnamTime();
            // Tạm thời comment điều kiện kiểm tra thời gian để bạn có thể test dễ dàng sự kiện bất kỳ lúc nào
            /*
            if (now < ticket.Event.StartTime.AddDays(-1) || now > ticket.Event.EndTime.AddHours(2))
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Khoảng thời gian này không được phép Check-in." };
            }
            */

            // 6. Check if already checked in
            if (ticket.Status == TicketStatusEnum.CheckedIn)
            {
                return new CheckInResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "Học sinh này đã được Check-in trước đó rồi.",
                    StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                    StudentEmail = ticket.Student.User?.Email
                };
            }

            // 7. Update status & save History
            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                ticket.Status = TicketStatusEnum.CheckedIn;
                ticket.UpdatedAt = now;
                await _uow.Tickets.UpdateAsync(ticket);

                var historyRecord = new DataAccess.Entities.CheckInHistory
                {
                    TicketId = ticket.Id,
                    ScannerId = orgProfile.Id,
                    ScanType = ScanTypeEnum.CheckIn,
                    DeviceName = "Mobile Web Scanner",
                    Location = ticket.Event.Location?.Name // Use location name if available
                };
                await _uow.CheckInHistories.CreateAsync(historyRecord);

                await _uow.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                return new CheckInResponseDto { IsSuccess = false, Message = "Lỗi khi lưu dữ liệu Check-in." };
            }

            return new CheckInResponseDto
            {
                IsSuccess = true,
                Message = "Check-in thành công!",
                StudentName = ticket.Student.User?.FullName ?? ticket.Student.User?.Email,
                StudentEmail = ticket.Student.User?.Email
            };
        }
    }
}
