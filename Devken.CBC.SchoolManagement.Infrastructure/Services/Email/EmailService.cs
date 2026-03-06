using Devken.CBC.SchoolManagement.Application.Service.Email;
using Devken.CBC.SchoolManagement.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Email
{
    public sealed class EmailService(
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger)
            : IEmailService
    {
        private readonly EmailSettings _cfg = options.Value;

        // ── IEmailService ─────────────────────────────────────────────────────

        public Task SendAsync(string toEmail, string subject, string htmlBody)
            => SendAsync(new[] { toEmail }, subject, htmlBody);

        public async Task SendAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody)
        {
            var recipients = toEmails.ToList();

            if (_cfg.UseConsoleInDevelopment)
            {
                _LogToConsole(recipients, subject, htmlBody);
                return;
            }

            using var message = _BuildMessage(recipients, subject, htmlBody);

            try
            {
                using var client = _BuildClient();
                await client.SendMailAsync(message);

                logger.LogInformation(
                    "[EmailService] Sent '{Subject}' to {Recipients}",
                    subject, string.Join(", ", recipients));
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[EmailService] Failed to send '{Subject}' to {Recipients}. Error: {Error}",
                    subject, string.Join(", ", recipients), ex.Message);

                throw;
            }
        }

        public Task SendTemplateAsync<TModel>(
            string toEmail,
            string subject,
            string templateName,
            TModel model)
        {
            var html = EmailTemplateEngine.Render(templateName, model);
            return SendAsync(toEmail, subject, html);
        }

        public Task SendOtpAsync(string toEmail, string firstName, string otp)
        {
            var subject = "Your Devken CBC verification code";
            var html = EmailTemplates.Otp(firstName, otp);
            return SendAsync(toEmail, subject, html);
        }

        public Task SendWelcomeAsync(string toEmail, string firstName)
        {
            var subject = "Welcome to Devken CBC School Management System";
            var html = EmailTemplates.Welcome(firstName);
            return SendAsync(toEmail, subject, html);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private MailMessage _BuildMessage(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody)
        {
            var msg = new MailMessage
            {
                From = new MailAddress(_cfg.FromAddress, _cfg.FromName, Encoding.UTF8),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
            };

            foreach (var addr in toEmails)
                msg.To.Add(addr);

            if (!string.IsNullOrWhiteSpace(_cfg.ReplyTo))
                msg.ReplyToList.Add(new MailAddress(_cfg.ReplyTo));

            return msg;
        }

        private SmtpClient _BuildClient() => new(_cfg.Host, _cfg.Port)
        {
            Credentials = new System.Net.NetworkCredential(_cfg.Username, _cfg.Password),
            EnableSsl = _cfg.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000,
        };

        private void _LogToConsole(
            IEnumerable<string> recipients,
            string subject,
            string body)
        {
            var separator = new string('─', 60);
            logger.LogInformation(
                "\n{Sep}\n[EmailService DEV] TO: {To}\nSUBJECT: {Subject}\n\n{Body}\n{Sep}",
                separator,
                string.Join(", ", recipients),
                subject,
                body,
                separator);
        }
    }
}
