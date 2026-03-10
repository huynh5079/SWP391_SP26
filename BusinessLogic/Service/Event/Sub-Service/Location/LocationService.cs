using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.Service.ValidationData.Loction;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Event.Sub_Service.Location
{
	public class LocationService : ILocationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILocationValidator _locationValidator;

		public LocationService(IUnitOfWork unitOfWork, ILocationValidator locationValidator)
		{
			_unitOfWork = unitOfWork;
			_locationValidator = locationValidator;
		}

		public async Task<List<LocationDTO>> GetAllLocationsAsync()
		{
			var locations = await _unitOfWork.Locations.GetAllAsync(x => x.DeletedAt == null);
			return locations
				.OrderBy(x => x.Name)
				.Select(MapLocation)
				.ToList();
		}

		public async Task<LocationDTO?> GetLocationByIdAsync(string locationId)
		{
			if (string.IsNullOrWhiteSpace(locationId))
			{
				return null;
			}

			var location = await _unitOfWork.Locations.GetAsync(x => x.Id == locationId && x.DeletedAt == null);
			return location == null ? null : MapLocation(location);
		}

		public async Task<List<LocationDTO>> GetAvailableLocationsAsync(DateTime startTime, DateTime endTime)
		{
			if (endTime <= startTime)
			{
				return new List<LocationDTO>();
			}

			var locations = await _unitOfWork.Locations.GetAllAsync(
				x => x.DeletedAt == null,
				q => q.Include(x => x.Events));

			return locations
				.Where(x => x.Status == DataAccess.Enum.LocationStatusEnum.Available)
				.Where(x => !(x.Events?.Any(e =>
					e.DeletedAt == null &&
					e.Status != DataAccess.Enum.EventStatusEnum.Cancelled &&
					e.StartTime < endTime &&
					startTime < e.EndTime) ?? false))
				.OrderBy(x => x.Name)
				.Select(MapLocation)
				.ToList();
		}

		public async Task<LocationDTO> CreateLocationAsync(CreateLocationDTO dto)
		{
			_locationValidator.ValidateCreateRequest(dto);

			var normalizedName = dto.Name.Trim();
			var normalizedAddress = dto.Address?.Trim() ?? string.Empty;
			var existingLocation = await _unitOfWork.Locations.GetAsync(
				x => x.Name == normalizedName
					&& (x.Address ?? string.Empty) == normalizedAddress
					&& x.DeletedAt == null);
			_locationValidator.ValidateDuplicateLocation(existingLocation);

			var now = DateTimeHelper.GetVietnamTime();
			var location = new DataAccess.Entities.Location
			{
				Id = Guid.NewGuid().ToString(),
				Name = normalizedName,
				Address = string.IsNullOrWhiteSpace(normalizedAddress) ? null : normalizedAddress,
				Capacity = dto.Capacity,
				Status = dto.Status,
				Type = dto.Type,
				Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
				CreatedAt = now,
				UpdatedAt = now,
				DeletedAt = null
			};

			await _unitOfWork.Locations.CreateAsync(location);
			await _unitOfWork.SaveChangesAsync();
			return MapLocation(location);
		}

		public async Task<bool> UpdateLocationAsync(string locationId, UpdateLocationDTO dto)
		{
			try
			{
				_locationValidator.ValidateUpdateRequest(locationId, dto);
			}
			catch (LocationValidator.BusinessValidationException)
			{
				return false;
			}

			var location = await _unitOfWork.Locations.GetAsync(x => x.Id == locationId && x.DeletedAt == null);
			try
			{
				_locationValidator.ValidateLocationExists(location);
			}
			catch (LocationValidator.BusinessValidationException)
			{
				return false;
			}

			var normalizedName = dto.Name.Trim();
			var normalizedAddress = dto.Address?.Trim() ?? string.Empty;
			var duplicateLocation = await _unitOfWork.Locations.GetAsync(
				x => x.Id != locationId
					&& x.Name == normalizedName
					&& (x.Address ?? string.Empty) == normalizedAddress
					&& x.DeletedAt == null);
			_locationValidator.ValidateDuplicateLocation(duplicateLocation);

			location!.Name = normalizedName;
			location.Address = string.IsNullOrWhiteSpace(normalizedAddress) ? null : normalizedAddress;
			location.Capacity = dto.Capacity;
			location.Status = dto.Status;
			location.Type = dto.Type;
			location.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
			location.UpdatedAt = DateTimeHelper.GetVietnamTime();

			await _unitOfWork.Locations.UpdateAsync(location);
			await _unitOfWork.SaveChangesAsync();
			return true;
		}

		private static LocationDTO MapLocation(DataAccess.Entities.Location location)
		{
			return new LocationDTO
			{
				LocationId = location.Id,
				Name = location.Name,
				Address = location.Address ?? string.Empty,
				Capacity = location.Capacity,
				Status = location.Status,
				Type = location.Type,
				Description = location.Description ?? string.Empty,
				CreatedAt = location.CreatedAt,
				UpdatedAt = location.UpdatedAt,
				DeletedAt = location.DeletedAt ?? DateTime.MinValue
			};
		}

		public async Task<bool> DeleteLocationAsync(string locationId)
		{
			if (string.IsNullOrWhiteSpace(locationId)) return false;

			var location = await _unitOfWork.Locations.GetAsync(x => x.Id == locationId && x.DeletedAt == null);
			_locationValidator.ValidateLocationExists(location);

			location!.DeletedAt = DateTimeHelper.GetVietnamTime();
			await _unitOfWork.Locations.UpdateAsync(location);
			await _unitOfWork.SaveChangesAsync();
			return true;
		}
	}
}
