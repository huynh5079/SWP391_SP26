using BusinessLogic.DTOs.Event.Location;

namespace BusinessLogic.Service.ValidationData.Loction
{
	public class LocationValidator : ILocationValidator
	{
		public void ValidateCreateRequest(CreateLocationDTO dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
			{
				throw new BusinessValidationException("Tên location không được để trống.");
			}

			if (!dto.Type.HasValue)
			{
				throw new BusinessValidationException("Loại location là bắt buộc.");
			}
		}

		public void ValidateUpdateRequest(string locationId, UpdateLocationDTO dto)
		{
			if (string.IsNullOrWhiteSpace(locationId) || dto == null || string.IsNullOrWhiteSpace(dto.Name))
			{
				throw new BusinessValidationException("Dữ liệu cập nhật location không hợp lệ.");
			}

			if (!dto.Type.HasValue)
			{
				throw new BusinessValidationException("Loại location là bắt buộc.");
			}
		}

		public void ValidateLocationExists(DataAccess.Entities.Location? location)
		{
			if (location == null)
			{
				throw new BusinessValidationException("Location không tồn tại.");
			}
		}

		public void ValidateDuplicateLocation(DataAccess.Entities.Location? duplicateLocation)
		{
			if (duplicateLocation != null)
			{
				throw new BusinessValidationException("Location đã tồn tại.");
			}
		}

		public class BusinessValidationException : Exception
		{
			public BusinessValidationException(string message) : base(message)
			{
			}
		}
	}
}
