using AEMS_Solution.Controllers.Api.Chatbot;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Api
{
    [Authorize]
    [Route("api/v1/lookup")]
    public class LookupApiController : ApiBaseController
    {
        private readonly IUnitOfWork _unitOfWork;

        public LookupApiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
            {
                return Success(new List<object>());
            }

            var query = q.Trim().ToLower();

            var users = await _unitOfWork.Users.GetAllAsync(
                u => u.DeletedAt == null && 
                     (u.Email.ToLower().Contains(query) || (u.FullName != null && u.FullName.ToLower().Contains(query))),
                includes: q => q.Include(u => u.Role)
            );

            var result = users
                .OrderBy(u => u.Email)
                .Take(10)
                .Select(u => new 
                {
                    id = u.Id,
                    fullName = u.FullName ?? "N/A",
                    email = u.Email,
                    role = u.Role?.RoleName.ToString() ?? "Unknown"
                })
                .ToList();

            return Success(result);
        }

        [HttpGet("locations")]
        public async Task<IActionResult> SearchLocations([FromQuery] string q)
        {
            var query = (q ?? "").Trim().ToLower();
            
            var locations = await _unitOfWork.Locations.GetAllAsync(
                l => l.DeletedAt == null && l.Status == DataAccess.Enum.LocationStatusEnum.Available &&
                     (l.Name.ToLower().Contains(query) || (l.Address != null && l.Address.ToLower().Contains(query)))
            );

            var result = locations
                .OrderBy(l => l.Name)
                .Take(20)
                .Select(l => new 
                {
                    id = l.Id,
                    name = l.Name,
                    address = l.Address,
                    capacity = l.Capacity
                })
                .ToList();

            return Success(result);
        }

        [HttpGet("topics")]
        public async Task<IActionResult> SearchTopics([FromQuery] string q)
        {
            var query = (q ?? "").Trim().ToLower();

            var topics = await _unitOfWork.Topics.GetAllAsync(
                t => t.DeletedAt == null && t.Name.ToLower().Contains(query)
            );

            var result = topics
                .OrderBy(t => t.Name)
                .Take(20)
                .Select(t => new 
                {
                    id = t.Id,
                    name = t.Name
                })
                .ToList();

            return Success(result);
        }
    }
}
