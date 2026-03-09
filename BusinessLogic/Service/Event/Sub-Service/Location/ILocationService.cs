using BusinessLogic.DTOs.Event.Location;

namespace BusinessLogic.Service.Event.Sub_Service.Location
{
	public interface ILocationService
	{
		Task<List<LocationDTO>> GetAllLocationsAsync();
		Task<LocationDTO?> GetLocationByIdAsync(string locationId);
		Task<List<LocationDTO>> GetAvailableLocationsAsync(DateTime startTime, DateTime endTime);
		Task<LocationDTO> CreateLocationAsync(CreateLocationDTO dto);
		Task<bool> UpdateLocationAsync(string locationId, UpdateLocationDTO dto);
	}
}
