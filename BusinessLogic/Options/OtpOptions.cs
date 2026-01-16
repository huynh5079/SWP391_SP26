using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Options
{
    public class OtpOptions
    {
        public int OtpExpireMinutes { get; set; } = 5;
        public int VerifiedFlagMinutes { get; set; } = 15;
        public int ResendCooldownSeconds { get; set; } = 30;
        public int MaxAttempts { get; set; } = 5;
    }
}
