// Infrastructure/Services/Email/EmailService.cs
using Devken.CBC.SchoolManagement.Application.Service.Email;
using Microsoft.Extensions.Logging;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Email
{
    /// <summary>
    /// Development-only stub — prints emails to the console instead of sending.
    /// Registered only when ASPNETCORE_ENVIRONMENT=Development.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        // ── Single recipient ──────────────────────────────────────────────────
        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            Log(toEmail, subject, htmlBody);
            return Task.CompletedTask;
        }

        // ── Multiple recipients ───────────────────────────────────────────────
        public Task SendAsync(IEnumerable<string> toEmails, string subject, string htmlBody)
        {
            foreach (var email in toEmails)
                Log(email, subject, htmlBody);

            return Task.CompletedTask;
        }

        // ── Template-based email ──────────────────────────────────────────────
        public Task SendTemplateAsync<TModel>(
            string toEmail,
            string subject,
            string templateName,
            TModel model)
        {
            var body = $"[TEMPLATE: {templateName}]\n{System.Text.Json.JsonSerializer.Serialize(model)}";
            Log(toEmail, subject, body);
            return Task.CompletedTask;
        }

        // ── OTP email ─────────────────────────────────────────────────────────
        public Task SendOtpAsync(string toEmail, string firstName, string otp)
        {
            var body = $"Hi {firstName}, your verification code is: {otp} (expires in 5 minutes)";
            Log(toEmail, "Your Devken CBC Verification Code", body);
            return Task.CompletedTask;
        }

        // ── Welcome email ─────────────────────────────────────────────────────
        public Task SendWelcomeAsync(string toEmail, string firstName)
        {
            var body = $"Hi {firstName}, welcome to Devken CBC School Management System! Your account is ready.";
            Log(toEmail, "Welcome to Devken CBC", body);
            return Task.CompletedTask;
        }

        // ── Password Reset email ──────────────────────────────────────────────
        public Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink)
        {
            var body = $@"
Hi {firstName},

A password reset was requested for your account.

Reset link (valid 30 minutes, single use):
{resetLink}

If you did not request this, ignore this email — no changes have been made.
";
            Log(toEmail, "Reset Your Devken CBC Password", body);
            return Task.CompletedTask;
        }

        // ── Private helper ────────────────────────────────────────────────────
        private void Log(string to, string subject, string body)
        {
            var separator = new string('─', 60);
            _logger.LogInformation(
                "\n{Sep}\n[EmailService DEV] TO: {To}\nSUBJECT: {Subject}\n{Body}\n{Sep}",
                separator, to, subject, body, separator);
        }
    }
}