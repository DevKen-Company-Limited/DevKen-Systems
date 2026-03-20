using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class SmtpSettings
    {
        public const string SectionName = "SmtpSettings";

        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string SenderName { get; set; } = "Devken CBC";
    }
}
