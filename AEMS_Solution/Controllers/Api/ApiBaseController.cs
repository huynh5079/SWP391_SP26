using BusinessLogic.Helper;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Api
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ApiBaseController : ControllerBase
    {
        protected string? CurrentUserId => User.GetUserId();

        protected IActionResult Success<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                Success = true,
                Message = message,
                Data = data
            });
        }

        protected IActionResult Error(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                Success = false,
                Message = message
            });
        }
    }
}
