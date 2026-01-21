using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Authentication.Password
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = default!;

        [Required]
        public string NewPassword { get; set; } = default!;

        [Required, Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = default!;
    }
}
