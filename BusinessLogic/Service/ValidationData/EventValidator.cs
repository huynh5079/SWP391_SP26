using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;

namespace BusinessLogic.Service.ValidationData
{
	public class EventValidator : IEventValidator
	{
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

			// 3) Rule theo Mode
			if (dto.Mode == null)
				throw new BusinessValidationException("Mode là bắt buộc.");

			if (dto.Mode == EventModeEnum.Online)
			{
				if (string.IsNullOrWhiteSpace(dto.MeetingUrl))
					throw new BusinessValidationException("Online event phải có MeetingUrl.");
			}
			else if (dto.Mode == EventModeEnum.Offline)
			{
				// Offline cần location
				if (string.IsNullOrWhiteSpace(dto.LocationId))
					throw new BusinessValidationException("Offline event phải có LocationId.");
			}
			else if (dto.Mode == EventModeEnum.Hybrid)
			{
				if (string.IsNullOrWhiteSpace(dto.LocationId))
					throw new BusinessValidationException("Hybrid event phải có LocationId.");
				if (string.IsNullOrWhiteSpace(dto.MeetingUrl))
					throw new BusinessValidationException("Hybrid event phải có MeetingUrl.");
			}

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
		}
		public class BusinessValidationException : Exception
		{
			public BusinessValidationException(string message) : base(message) { }
		}
		
	}
}

