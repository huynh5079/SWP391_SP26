using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Authentication.Login
{
    public class LoggedInUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = default!;
    }
}
