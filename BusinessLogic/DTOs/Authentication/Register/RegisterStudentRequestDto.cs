using DataAccess.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs.Authentication.Register
{
    public class RegisterStudentRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        [Required]
        public string FullName { get; set; } = default!;

        [Required]
        public string StudentCode { get; set; } = default!;

        public string? Phone { get; set; }

        public Gender? Gender { get; set; }

        public string? Major { get; set; }
    }
}
