// Application/Service/Email/IEmailService.cs
namespace Devken.CBC.SchoolManagement.Application.Service.Email
{
    public interface IEmailService
    {
        // ── Generic sends ─────────────────────────────────────────────────────
        Task SendAsync(string toEmail, string subject, string htmlBody);
        Task SendAsync(IEnumerable<string> toEmails, string subject, string htmlBody);

        // ── Template-based ────────────────────────────────────────────────────
        Task SendTemplateAsync<TModel>(
            string toEmail, string subject, string templateName, TModel model);

        // ── Typed transactional emails ────────────────────────────────────────
        Task SendOtpAsync(string toEmail, string firstName, string otp);
        Task SendWelcomeAsync(string toEmail, string firstName);

        /// <summary>
        /// Sends a password-reset email containing a secure link built from
        /// <paramref name="resetLink"/>.  The link must be a fully-qualified HTTPS URL
        /// that includes the raw reset token as a query-string parameter, e.g.:
        ///   https://app.devkencbc.com/reset-password?token=&lt;rawToken&gt;
        /// </summary>
        Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink);
    }
}