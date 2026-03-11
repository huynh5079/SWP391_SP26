using AEMS_Solution.Controllers.Api;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Api
{
    [Authorize]
    [Route("api/v1/[controller]")]
    public class UserApiController : ApiBaseController
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserApiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Success(new List<object>());
            }

            var query = q.Trim().ToLower();

            // Fetch users matching the query string in Email or FullName, ignoring deleted accounts
            var users = await _unitOfWork.Users.GetAllAsync(
                u => u.DeletedAt == null && 
                     (u.Email.ToLower().Contains(query) || (u.FullName != null && u.FullName.ToLower().Contains(query))),
                includes: q => q.Include(u => u.Role)
            );

            // Using LINQ to projection, take top 10 results to prevent large payload
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
    }
}
