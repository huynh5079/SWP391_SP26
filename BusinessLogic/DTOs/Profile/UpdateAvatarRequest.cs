using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Profile
{
    public class UpdateAvatarRequest
    {
        public IFormFile Avatar { get; set; } = null!;
    }
}
