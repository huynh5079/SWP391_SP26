using BusinessLogic.DTOs.Ticket;
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
		public CheckInService(IUnitOfWork uow,IOrganizerValidator validator)
        {
            _uow = uow;
            _validator = validator;
        }

        public async Task<CheckInResponseDto> ProcessCheckInAsync(CheckInRequestDto request, string organizerUserId)
        {
            // 1. Validate payload
            if (string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
            {
                _validator.ValidateCheckInRequest(request);
			}

            // 2. Resolve Organizer Profile from UserId
            var orgProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId);
            if (orgProfile == null)
            {
                _validator.ValidateOrganizerCanCheckIn(orgProfile);
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
                _validator.ValidateTicketForCheckIn(ticket);
			}

            // 4. Validate Event Ownership
             _validator.ValidateEventOwnership(ticket, orgProfile);
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
			_validator.ValidateNotAlreadyCheckedIn(ticket);
			_validator.ValidateCheckInWindow(ticket, now);

			// 7. Update status & save History
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
			catch
			{
				await transaction.RollbackAsync();
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
            // 1. Validate payload
            if (string.IsNullOrWhiteSpace(request.QrPayload) || string.IsNullOrWhiteSpace(request.EventId))
            {
                _validator.ValidateCheckInRequest(request);
            }

            // 2. Resolve Organizer Profile
            var orgProfile = await _uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId);
            if (orgProfile == null)
            {
                _validator.ValidateOrganizerCanCheckIn(orgProfile);
            }

            // 3. Find the ticket
            var ticket = await _uow.Tickets.GetAsync(
                t => t.Id == request.QrPayload && t.EventId == request.EventId,
                q => q.Include(t => t.Event).Include(t => t.Student).ThenInclude(s => s.User));

            if (ticket == null || ticket.DeletedAt != null || ticket.Status == TicketStatusEnum.Cancelled)
            {
                _validator.ValidateTicketForCheckIn(ticket);
            }

            // 4. Validate Event Ownership
            _validator.ValidateEventOwnership(ticket, orgProfile);

            // 5. Must be in CheckedIn status to checkout
            if (ticket.Status != TicketStatusEnum.CheckedIn)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Sinh viên này chưa Check-in hoặc đã Checkout." };
            }

            // 6. Find the CheckInHistory record
            var checkInRecord = await _uow.CheckInHistories
                .GetAsync(h => h.TicketId == ticket.Id && h.CheckoutTime == null, 
                          q => q.OrderByDescending(h => h.CreatedAt));

            if (checkInRecord == null)
            {
                return new CheckInResponseDto { IsSuccess = false, Message = "Không tìm thấy thông tin Check-in hợp lệ để Checkout." };
            }

            // 7. Update status & save History
            using var transaction = await _uow.BeginTransactionAsync();
            var now = DateTimeHelper.GetVietnamTime();
            try
            {
                // Update Ticket status
                ticket.Status = TicketStatusEnum.Used; // Or a new status e.g., "Completed"
                ticket.UpdatedAt = now;
                await _uow.Tickets.UpdateAsync(ticket);

                // Update CheckInHistory with checkout time
                checkInRecord.CheckoutTime = now;
                checkInRecord.UpdatedAt = now;
                await _uow.CheckInHistories.UpdateAsync(checkInRecord);

                await _uow.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
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
    }
}
