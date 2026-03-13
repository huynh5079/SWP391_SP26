using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Ticket;
using BusinessLogic.Service.ValidationData.Ticket;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BusinessLogic.Service.Event.Sub_Service.Ticket
{
	public class TicketService : ITicketService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ITicketValidator _ticketValidator;

		public TicketService(IUnitOfWork unitOfWork, ITicketValidator ticketValidator)
		{
			_unitOfWork = unitOfWork;
			_ticketValidator = ticketValidator;
		}

		public async Task<List<TicketDTO>> GetAllTicketsAsync()
		{
			var tickets = await _unitOfWork.Tickets.GetAllAsync(
				x => x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student)
						.ThenInclude(x => x!.User));

			return tickets
				.OrderByDescending(x => x.CreatedAt)
				.Select(MapTicket)
				.ToList();
		}

		public async Task<TicketDTO?> GetTicketByIdAsync(string ticketId)
		{
			if (string.IsNullOrWhiteSpace(ticketId))
			{
				return null;
			}

			var ticket = await _unitOfWork.Tickets.GetAsync(
				x => x.Id == ticketId && x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student)
						.ThenInclude(x => x!.User));

			return ticket == null ? null : MapTicket(ticket);
		}

		public async Task<List<TicketDTO>> GetTicketsByEventAsync(string eventId)
		{
			if (string.IsNullOrWhiteSpace(eventId))
			{
				return new List<TicketDTO>();
			}

			var tickets = await _unitOfWork.Tickets.GetAllAsync(
				x => x.EventId == eventId && x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student)
						.ThenInclude(x => x!.User));

			return tickets
				.OrderByDescending(x => x.CreatedAt)
				.Select(MapTicket)
				.ToList();
		}

		public async Task<List<TicketDTO>> GetTicketsByStudentAsync(string studentId)
		{
			if (string.IsNullOrWhiteSpace(studentId))
			{
				return new List<TicketDTO>();
			}

			var tickets = await _unitOfWork.Tickets.GetAllAsync(
				x => x.StudentId == studentId && x.DeletedAt == null,
				q => q.Include(x => x.Event)
					.Include(x => x.Student)
						.ThenInclude(x => x!.User));

			return tickets
				.OrderByDescending(x => x.CreatedAt)
				.Select(MapTicket)
				.ToList();
		}

		public async Task<TicketDTO> CreateTicketAsync(CreateTicketDTO dto)
		{
			_ticketValidator.ValidateCreateRequest(dto);

			using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
			try
			{
				var eventEntity = await _unitOfWork.Events.GetAsync(x => x.Id == dto.EventId && x.DeletedAt == null);
				_ticketValidator.ValidateEventExists(eventEntity);

				var student = await _unitOfWork.StudentProfiles.GetAsync(
					x => x.Id == dto.StudentId && x.DeletedAt == null,
					q => q.Include(x => x.User));
				_ticketValidator.ValidateStudentExists(student);

				var existingTicket = await _unitOfWork.Tickets.GetAsync(
					x => x.EventId == dto.EventId && x.StudentId == dto.StudentId && x.DeletedAt == null && x.Status != TicketStatusEnum.Cancelled);
				_ticketValidator.ValidateDuplicateActiveTicket(existingTicket);

				var activeTicketCount = await _unitOfWork.Tickets.CountAsync(
					x => x.EventId == dto.EventId && x.DeletedAt == null && x.Status != TicketStatusEnum.Cancelled);
				if (activeTicketCount >= eventEntity!.MaxCapacity)
				{
					throw new InvalidOperationException("Event đã đủ sức chứa.");
				}

				var now = DateTimeHelper.GetVietnamTime();
				var ticket = new DataAccess.Entities.Ticket
				{
					Id = Guid.NewGuid().ToString(),
					EventId = dto.EventId,
					StudentId = dto.StudentId,
					TicketCode = string.IsNullOrWhiteSpace(dto.TicketCode) ? GenerateTicketCode() : dto.TicketCode.Trim(),
					Status = dto.Status,
					CheckInTime = dto.Status == TicketStatusEnum.CheckedIn ? now : null
				};

				await _unitOfWork.Tickets.CreateAsync(ticket);
				await _unitOfWork.SaveChangesAsync();
				await transaction.CommitAsync();

				ticket.Event = eventEntity;
				ticket.Student = student!;
				return MapTicket(ticket);
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task<bool> UpdateTicketAsync(string ticketId, UpdateTicketDTO dto)
		{
			try
			{
				_ticketValidator.ValidateUpdateRequest(ticketId);
			}
			catch (TicketValidator.BusinessValidationException)
			{
				return false;
			}

			var ticket = await _unitOfWork.Tickets.GetAsync(x => x.Id == ticketId && x.DeletedAt == null);
			try
			{
				_ticketValidator.ValidateTicketExists(ticket);
			}
			catch (TicketValidator.BusinessValidationException)
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(dto.TicketCode))
			{
				ticket!.TicketCode = dto.TicketCode.Trim();
			}

			if (dto.Status.HasValue)
			{
				ticket!.Status = dto.Status.Value;
			}

			if (dto.CheckInTime.HasValue)
			{
				ticket!.CheckInTime = dto.CheckInTime.Value;
			}
			else if (dto.Status == TicketStatusEnum.CheckedIn && ticket!.CheckInTime == null)
			{
				ticket.CheckInTime = DateTimeHelper.GetVietnamTime();
			}
			else if (dto.Status == TicketStatusEnum.Cancelled)
			{
				ticket!.CheckInTime = null;
			}

			ticket!.UpdatedAt = DateTimeHelper.GetVietnamTime();

			await _unitOfWork.Tickets.UpdateAsync(ticket);
			await _unitOfWork.SaveChangesAsync();
			return true;
		}

		private static TicketDTO MapTicket(DataAccess.Entities.Ticket ticket)
		{
			var studentName = ticket.Student?.User?.FullName ?? ticket.Student?.StudentCode ?? string.Empty;
			return new TicketDTO
			{
				Id = ticket.Id,
				EventId = ticket.EventId,
				StudentId = ticket.StudentId,
				EventName = ticket.Event?.Title,
				TicketCode = ticket.TicketCode,
				Status = ticket.Status,
				CheckInTime = ticket.CheckInTime,
				StudentName = studentName,
			};
		}

		private static string GenerateTicketCode()
		{
			return $"TCK-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
		}
	}
}
