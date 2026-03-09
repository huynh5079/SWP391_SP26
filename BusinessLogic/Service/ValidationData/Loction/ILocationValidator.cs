using BusinessLogic.DTOs.Event.Location;

namespace BusinessLogic.Service.ValidationData.Loction
{
	public interface ILocationValidator
	{
		void ValidateCreateRequest(CreateLocationDTO dto);
		void ValidateUpdateRequest(string locationId, UpdateLocationDTO dto);
		void ValidateLocationExists(DataAccess.Entities.Location? location);
		void ValidateDuplicateLocation(DataAccess.Entities.Location? duplicateLocation);
	}
}
