using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.Service.ValidationData.Event
{
	public class EventValidator : IEventValidator
	{
		private static void ValidateMode(EventModeEnum? mode, string? locationId, string? meetingUrl)
		{
			if (mode == null)
				throw new BusinessValidationException("Mode là bắt buộc.");

			if (mode == EventModeEnum.Online)
			{
				if (!string.IsNullOrWhiteSpace(locationId))
					throw new BusinessValidationException("Online event không được có LocationId.");
				if (string.IsNullOrWhiteSpace(meetingUrl))
					throw new BusinessValidationException("Online event phải có MeetingUrl.");
			}
			else if (mode == EventModeEnum.Offline)
			{
				if (string.IsNullOrWhiteSpace(locationId))
					throw new BusinessValidationException("Offline event phải có LocationId.");
				if (!string.IsNullOrWhiteSpace(meetingUrl))
					throw new BusinessValidationException("Offline event không được có MeetingUrl.");
			}
			else if (mode == EventModeEnum.Hybrid)
			{
				if (string.IsNullOrWhiteSpace(locationId))
					throw new BusinessValidationException("Hybrid event phải có LocationId.");
				if (string.IsNullOrWhiteSpace(meetingUrl))
					throw new BusinessValidationException("Hybrid event phải có MeetingUrl.");
			}
		}

		//Valid create event
		public void ValidateCreate(CreateEventRequestDto dto)
		{
			// 1) Rule thời gian
			if (dto.StartTime >= dto.EndTime)
				throw new BusinessValidationException("StartTime phải nhỏ hơn EndTime.");

			if (dto.RegistrationOpenTime >= dto.RegistrationCloseTime)
				throw new BusinessValidationException("RegistrationOpenTime phải nhỏ hơn RegistrationCloseTime.");

			if (dto.RegistrationCloseTime > dto.StartTime)
				throw new BusinessValidationException("RegistrationCloseTime phải <= StartTime.");

			// 2) Capacity
			if (dto.Capacity <= 0)
				throw new BusinessValidationException("Capacity phải > 0.");

			ValidateMode(dto.Mode, dto.LocationId, dto.MeetingUrl);

			// 4) Topic/Location bắt buộc (nếu bạn luôn cần)
			if (string.IsNullOrWhiteSpace(dto.TopicId))
				throw new BusinessValidationException("TopicId là bắt buộc.");
		}
		//Valid update event
		public void ValidateUpdate(UpdateEventRequestDto dto)
		{
			// Update thì có thể reuse rule create nếu bạn muốn
			if (dto.StartTime >= dto.EndTime)
				throw new BusinessValidationException("StartTime phải nhỏ hơn EndTime.");

			ValidateMode(dto.Mode, dto.LocationId, dto.MeetingUrl);
		}

		public void ValidateAgendas(List<CreateAgendaItemDto>? agendas)
		{
			if (agendas == null) return;
			foreach (var a in agendas)
			{
				bool isEmpty = string.IsNullOrWhiteSpace(a.SessionName) && string.IsNullOrWhiteSpace(a.SpeakerInfo)
					&& string.IsNullOrWhiteSpace(a.Description) && a.StartTime == null && a.EndTime == null && string.IsNullOrWhiteSpace(a.Location);
				if (isEmpty) continue;

				if (a.StartTime != null && a.EndTime != null && a.EndTime <= a.StartTime)
					throw new BusinessValidationException("Agenda EndTime phải lớn hơn StartTime");
			}
		}

		public void ValidateDeposit(CreateEventRequestDto dto)
		{
			if (dto.IsDepositRequired && dto.DepositAmount <= 0)
				throw new BusinessValidationException("DepositAmount phải lớn hơn 0 khi deposit được yêu cầu.");
		}
		public class BusinessValidationException : Exception
		{
			public BusinessValidationException(string message) : base(message) { }
		}
		
	}
}

