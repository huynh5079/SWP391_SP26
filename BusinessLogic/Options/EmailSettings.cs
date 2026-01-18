using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Options
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = default!;
        public int Port { get; set; }
        public string SenderName { get; set; } = default!;
        public string SenderEmail { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
