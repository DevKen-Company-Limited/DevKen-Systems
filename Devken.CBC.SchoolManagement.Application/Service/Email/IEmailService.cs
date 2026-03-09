using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Email
{
    public interface IEmailService
    {
        /// <summary>Send a plain HTML email to a single recipient.</summary>
        Task SendAsync(string toEmail, string subject, string htmlBody);

        /// <summary>Send to multiple recipients.</summary>
        Task SendAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody);

        /// <summary>Send using a named template with model substitution.</summary>
        Task SendTemplateAsync<TModel>(
            string toEmail,
            string subject,
            string templateName,
            TModel model);

        /// <summary>Dedicated OTP email — preferred over raw SendAsync for OTPs.</summary>
        Task SendOtpAsync(string toEmail, string firstName, string otp);

        /// <summary>Welcome email sent after a new SSO user sets their password.</summary>
        Task SendWelcomeAsync(string toEmail, string firstName);
    }
}
