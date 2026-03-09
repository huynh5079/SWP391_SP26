using System.Security.Claims;

namespace AEMS_Solution.Middlewares.Extensions
{
    public static class ClaimsExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string? GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}
