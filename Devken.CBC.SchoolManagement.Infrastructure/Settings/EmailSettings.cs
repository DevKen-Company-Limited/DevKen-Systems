using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Settings
{
    public class EmailSettings
    {
        public const string SectionName = "Email";

        /// <summary>SMTP host e.g. smtp.gmail.com or smtp.sendgrid.net</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>SMTP port. 587 = STARTTLS, 465 = SSL, 25 = plain (dev only)</summary>
        public int Port { get; set; } = 587;

        /// <summary>Enable SSL/TLS (true for port 465, false for 587 STARTTLS)</summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>SMTP username / API key user</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>SMTP password / API key</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>From address shown in the email client</summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>From display name shown in the email client</summary>
        public string FromName { get; set; } = "Devken CBC";

        /// <summary>
        /// When true, emails are written to the console instead of sent.
        /// Set to true in Development to avoid sending real emails.
        /// </summary>
        public bool UseConsoleInDevelopment { get; set; } = true;

        /// <summary>Optional reply-to address</summary>
        public string? ReplyTo { get; set; }
    }
}
