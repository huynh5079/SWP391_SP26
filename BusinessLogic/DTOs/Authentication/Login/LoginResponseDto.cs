using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Authentication.Login
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = default!;
        public LoggedInUserDto User { get; set; } = default!;
    }
}
