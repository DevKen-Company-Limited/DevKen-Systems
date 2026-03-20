// Infrastructure/Services/Email/SmtpEmailService.cs
using Devken.CBC.SchoolManagement.Application.Service.Email;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(SmtpSettings settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        // ── Single recipient ──────────────────────────────────────────────────
        public async Task SendAsync(string toEmail, string subject, string htmlBody)
            => await SendCoreAsync(new[] { toEmail }, subject, htmlBody);

        // ── Multiple recipients ───────────────────────────────────────────────
        public async Task SendAsync(IEnumerable<string> toEmails, string subject, string htmlBody)
            => await SendCoreAsync(toEmails, subject, htmlBody);

        // ── Template-based email ──────────────────────────────────────────────
        public async Task SendTemplateAsync<TModel>(
            string toEmail,
            string subject,
            string templateName,
            TModel model)
        {
            var html = await LoadTemplateAsync(templateName);
            html = SubstituteTokens(html, model);
            await SendCoreAsync(new[] { toEmail }, subject, html);
        }

        // ── OTP email ─────────────────────────────────────────────────────────
        public async Task SendOtpAsync(string toEmail, string firstName, string otp)
        {
            const string subject = "Your Devken CBC Verification Code";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>Verification Code</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f6fb;font-family:Georgia,""Times New Roman"",Times,serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
         style='background-color:#f4f6fb;padding:40px 16px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' style='max-width:560px;' cellpadding='0' cellspacing='0'>

          <!-- Header -->
          <tr>
            <td style='background:linear-gradient(135deg,#0f2d6b 0%,#1a4db8 100%);
                        border-radius:12px 12px 0 0;padding:32px 40px;text-align:center;'>
              <div style='display:inline-block;width:48px;height:48px;border-radius:50%;
                           background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.35);
                           line-height:48px;font-size:22px;color:#ffffff;font-weight:bold;
                           margin-bottom:14px;text-align:center;'>D</div>
              <div style='color:#ffffff;font-size:20px;font-weight:bold;letter-spacing:0.5px;'>Devken CBC</div>
              <div style='color:rgba(255,255,255,0.65);font-size:12px;letter-spacing:2px;
                           text-transform:uppercase;margin-top:4px;'>School Management System</div>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='background:#ffffff;padding:40px 40px 32px;'>
              <p style='margin:0 0 8px;font-size:24px;color:#0f2d6b;font-weight:bold;'>Hello, {firstName}</p>
              <p style='margin:0 0 28px;font-size:15px;color:#475569;line-height:1.6;'>
                We received a request to verify your identity. Use the code below
                to complete your sign-in. Do not share this code with anyone.
              </p>
              <div style='height:1px;background:linear-gradient(90deg,transparent,#cbd5e1,transparent);
                           margin-bottom:28px;'></div>
              <p style='margin:0 0 12px;font-size:12px;color:#94a3b8;letter-spacing:2px;
                          text-transform:uppercase;text-align:center;'>Your one-time code</p>
              <div style='background:#f0f5ff;border:2px dashed #93c5fd;border-radius:10px;
                           padding:20px 24px;text-align:center;margin-bottom:28px;'>
                <span style='font-family:""Courier New"",Courier,monospace;font-size:44px;
                              font-weight:bold;letter-spacing:14px;color:#0f2d6b;display:inline-block;'>
                  {otp}
                </span>
              </div>
              <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
                     style='background:#fff7ed;border-left:4px solid #f59e0b;
                             border-radius:0 6px 6px 0;margin-bottom:28px;'>
                <tr>
                  <td style='padding:12px 16px;font-size:13px;color:#92400e;line-height:1.5;'>
                    &#x23F1;&nbsp; This code expires in <strong>5 minutes</strong> and is valid for a single use only.
                  </td>
                </tr>
              </table>
              <p style='margin:0;font-size:13px;color:#94a3b8;line-height:1.6;'>
                If you did not request this code, no action is needed — your account remains secure.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='background:#0f2d6b;border-radius:0 0 12px 12px;padding:20px 40px;text-align:center;'>
              <p style='margin:0 0 4px;color:rgba(255,255,255,0.5);font-size:12px;'>
                &copy; {DateTime.UtcNow.Year} Devken CBC School Management System. All rights reserved.
              </p>
              <p style='margin:0;color:rgba(255,255,255,0.35);font-size:11px;'>
                This is an automated message &mdash; please do not reply.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
            await SendCoreAsync(new[] { toEmail }, subject, body);
        }

        // ── Welcome email ─────────────────────────────────────────────────────
        public async Task SendWelcomeAsync(string toEmail, string firstName)
        {
            const string subject = "Welcome to Devken CBC School Management";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>Welcome</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f6fb;font-family:Georgia,""Times New Roman"",Times,serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
         style='background-color:#f4f6fb;padding:40px 16px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' style='max-width:560px;' cellpadding='0' cellspacing='0'>

          <!-- Hero header -->
          <tr>
            <td style='background:linear-gradient(135deg,#0f2d6b 0%,#1a4db8 60%,#2563eb 100%);
                        border-radius:12px 12px 0 0;padding:48px 40px 40px;text-align:center;'>
              <div style='display:inline-block;width:56px;height:56px;border-radius:50%;
                           background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.4);
                           line-height:56px;font-size:26px;color:#ffffff;font-weight:bold;
                           margin-bottom:16px;text-align:center;'>D</div>
              <div style='color:#ffffff;font-size:26px;font-weight:bold;letter-spacing:0.5px;margin-bottom:6px;'>
                Devken CBC
              </div>
              <div style='color:rgba(255,255,255,0.6);font-size:12px;letter-spacing:2.5px;text-transform:uppercase;'>
                School Management System
              </div>
              <div style='width:48px;height:3px;background:#f59e0b;border-radius:2px;margin:20px auto 0;'></div>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='background:#ffffff;padding:40px 40px 32px;'>
              <p style='margin:0 0 4px;font-size:13px;color:#94a3b8;letter-spacing:2px;text-transform:uppercase;'>
                Account Activated
              </p>
              <p style='margin:0 0 20px;font-size:26px;color:#0f2d6b;font-weight:bold;line-height:1.25;'>
                Welcome aboard, {firstName}!
              </p>
              <div style='height:1px;background:linear-gradient(90deg,transparent,#cbd5e1,transparent);
                           margin-bottom:24px;'></div>
              <p style='margin:0 0 16px;font-size:15px;color:#475569;line-height:1.7;'>
                Your account on the <strong style='color:#0f2d6b;'>Devken CBC School Management System</strong>
                has been successfully created and is ready to use.
              </p>
              <p style='margin:0 0 28px;font-size:15px;color:#475569;line-height:1.7;'>
                You can now sign in with your registered credentials to access the platform.
              </p>
              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:28px;'>
                <tr>
                  <td style='padding:12px 16px;background:#f8fafc;border-radius:8px 8px 0 0;
                               border-bottom:1px solid #e2e8f0;font-size:13px;color:#334155;'>
                    &#x1F4DA;&nbsp; <strong>Academic records</strong> &mdash; manage grades, reports &amp; transcripts
                  </td>
                </tr>
                <tr>
                  <td style='padding:12px 16px;background:#f8fafc;border-bottom:1px solid #e2e8f0;
                               font-size:13px;color:#334155;'>
                    &#x1F465;&nbsp; <strong>Student profiles</strong> &mdash; enrollment, attendance &amp; performance
                  </td>
                </tr>
                <tr>
                  <td style='padding:12px 16px;background:#f8fafc;border-radius:0 0 8px 8px;
                               font-size:13px;color:#334155;'>
                    &#x1F514;&nbsp; <strong>Notifications</strong> &mdash; real-time alerts &amp; communications
                  </td>
                </tr>
              </table>
              <div style='text-align:center;margin-bottom:28px;'>
                <a href='#' style='display:inline-block;background:linear-gradient(135deg,#0f2d6b,#1a4db8);
                                    color:#ffffff;text-decoration:none;font-size:15px;font-weight:bold;
                                    padding:14px 36px;border-radius:8px;letter-spacing:0.5px;'>
                  Sign In to Your Account &#x2192;
                </a>
              </div>
              <p style='margin:0;font-size:13px;color:#94a3b8;line-height:1.6;text-align:center;'>
                Need help? Reply to this email or contact our support team &mdash; we&rsquo;re happy to assist.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='background:#0f2d6b;border-radius:0 0 12px 12px;padding:20px 40px;text-align:center;'>
              <p style='margin:0 0 4px;color:rgba(255,255,255,0.5);font-size:12px;'>
                &copy; {DateTime.UtcNow.Year} Devken CBC School Management System. All rights reserved.
              </p>
              <p style='margin:0;color:rgba(255,255,255,0.35);font-size:11px;'>
                This is an automated message &mdash; please do not reply directly.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
            await SendCoreAsync(new[] { toEmail }, subject, body);
        }

        // ── Password Reset email ──────────────────────────────────────────────
        public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink)
        {
            const string subject = "Reset Your Devken CBC Password";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>Password Reset</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f6fb;font-family:Georgia,""Times New Roman"",Times,serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
         style='background-color:#f4f6fb;padding:40px 16px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' style='max-width:560px;' cellpadding='0' cellspacing='0'>

          <!-- Header -->
          <tr>
            <td style='background:linear-gradient(135deg,#0f2d6b 0%,#1a4db8 100%);
                        border-radius:12px 12px 0 0;padding:32px 40px;text-align:center;'>
              <div style='display:inline-block;width:48px;height:48px;border-radius:50%;
                           background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.35);
                           line-height:48px;font-size:22px;color:#ffffff;font-weight:bold;
                           margin-bottom:14px;text-align:center;'>D</div>
              <div style='color:#ffffff;font-size:20px;font-weight:bold;letter-spacing:0.5px;'>Devken CBC</div>
              <div style='color:rgba(255,255,255,0.65);font-size:12px;letter-spacing:2px;
                           text-transform:uppercase;margin-top:4px;'>School Management System</div>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='background:#ffffff;padding:40px 40px 32px;'>

              <!-- Icon row -->
              <div style='text-align:center;margin-bottom:24px;'>
                <div style='display:inline-block;width:56px;height:56px;border-radius:50%;
                             background:#fef3c7;border:2px solid #fde68a;line-height:56px;
                             font-size:26px;text-align:center;'>&#x1F510;</div>
              </div>

              <!-- Heading -->
              <p style='margin:0 0 4px;font-size:13px;color:#94a3b8;letter-spacing:2px;
                          text-transform:uppercase;text-align:center;'>Password Reset</p>
              <p style='margin:0 0 20px;font-size:24px;color:#0f2d6b;font-weight:bold;
                          text-align:center;line-height:1.3;'>
                Reset your password, {firstName}
              </p>

              <div style='height:1px;background:linear-gradient(90deg,transparent,#cbd5e1,transparent);
                           margin-bottom:24px;'></div>

              <p style='margin:0 0 24px;font-size:15px;color:#475569;line-height:1.7;'>
                We received a request to reset the password for your Devken CBC account.
                Click the button below to choose a new password. This link is valid for
                <strong>30 minutes</strong> and can only be used once.
              </p>

              <!-- CTA button -->
              <div style='text-align:center;margin-bottom:28px;'>
                <a href='{resetLink}'
                   style='display:inline-block;background:linear-gradient(135deg,#0f2d6b,#1a4db8);
                           color:#ffffff;text-decoration:none;font-size:15px;font-weight:bold;
                           padding:15px 40px;border-radius:8px;letter-spacing:0.5px;'>
                  Reset My Password &#x2192;
                </a>
              </div>

              <!-- Fallback link -->
              <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
                     style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;
                             margin-bottom:28px;'>
                <tr>
                  <td style='padding:14px 16px;'>
                    <p style='margin:0 0 6px;font-size:12px;color:#94a3b8;letter-spacing:1px;
                                text-transform:uppercase;'>Button not working?</p>
                    <p style='margin:0 0 6px;font-size:13px;color:#475569;'>
                      Copy and paste this link into your browser:
                    </p>
                    <p style='margin:0;font-size:12px;color:#1a4db8;word-break:break-all;
                                font-family:""Courier New"",Courier,monospace;'>
                      {resetLink}
                    </p>
                  </td>
                </tr>
              </table>

              <!-- Security warning -->
              <table role='presentation' width='100%' cellpadding='0' cellspacing='0'
                     style='background:#fef2f2;border-left:4px solid #f87171;
                             border-radius:0 6px 6px 0;margin-bottom:28px;'>
                <tr>
                  <td style='padding:12px 16px;font-size:13px;color:#991b1b;line-height:1.5;'>
                    &#x26A0;&#xFE0F;&nbsp; If you did <strong>not</strong> request a password reset,
                    please ignore this email. Your account is safe — no changes have been made.
                  </td>
                </tr>
              </table>

              <p style='margin:0;font-size:13px;color:#94a3b8;line-height:1.6;'>
                For security, this link will expire automatically after 30 minutes. After it expires,
                you can request a new password-reset link from the sign-in page.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='background:#0f2d6b;border-radius:0 0 12px 12px;padding:20px 40px;text-align:center;'>
              <p style='margin:0 0 4px;color:rgba(255,255,255,0.5);font-size:12px;'>
                &copy; {DateTime.UtcNow.Year} Devken CBC School Management System. All rights reserved.
              </p>
              <p style='margin:0;color:rgba(255,255,255,0.35);font-size:11px;'>
                This is an automated message &mdash; please do not reply.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
            await SendCoreAsync(new[] { toEmail }, subject, body);
        }

        // ── Core SMTP dispatcher ──────────────────────────────────────────────
        private async Task SendCoreAsync(
            IEnumerable<string> toEmails,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException(
                    "SMTP host is not configured. Set SmtpSettings:Host in appsettings " +
                    "or via environment variables.");

            var recipients = toEmails.ToList();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            foreach (var email in recipients)
                message.To.Add(MailboxAddress.Parse(email));

            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "[EmailService] ✅ Sent — To: {Recipients} | Subject: '{Subject}'",
                string.Join(", ", recipients), subject);
        }

        // ── Template helpers ──────────────────────────────────────────────────
        private static async Task<string> LoadTemplateAsync(string templateName)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory, "EmailTemplates", $"{templateName}.html");

            return File.Exists(path)
                ? await File.ReadAllTextAsync(path)
                : $"<p>Template <strong>{templateName}</strong> not found.</p>";
        }

        private static string SubstituteTokens<TModel>(string template, TModel model)
        {
            if (model == null) return template;

            foreach (var prop in typeof(TModel).GetProperties())
            {
                var token = $"{{{{{prop.Name}}}}}";
                var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                template = template.Replace(token, value, StringComparison.OrdinalIgnoreCase);
            }

            return template;
        }
    }
}