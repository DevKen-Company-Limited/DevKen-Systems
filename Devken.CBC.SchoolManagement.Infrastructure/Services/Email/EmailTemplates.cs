using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Email
{
    /// <summary>
    /// Inline HTML email templates.
    /// All templates share the same branded wrapper so they are consistent.
    /// </summary>
    public static class EmailTemplates
    {
        // ── OTP ───────────────────────────────────────────────────────────────

        public static string Otp(string firstName, string otp) => Wrap($@"
            <p style='margin:0 0 16px;font-size:15px;color:#374151;'>
                Hi <strong>{Esc(firstName)}</strong>,
            </p>
            <p style='margin:0 0 24px;font-size:15px;color:#374151;line-height:1.6;'>
                Use the verification code below to complete your Google sign-in to
                <strong>Devken CBC School Management System</strong>.
            </p>

            <!-- OTP box -->
            <div style='
                margin: 0 auto 24px;
                padding: 24px 32px;
                background: #eff6ff;
                border: 2px solid #bfdbfe;
                border-radius: 12px;
                text-align: center;
                max-width: 280px;
            '>
                <div style='
                    font-size: 42px;
                    font-weight: 800;
                    letter-spacing: 12px;
                    color: #1e3a8a;
                    font-family: monospace;
                '>{Esc(otp)}</div>
                <div style='
                    margin-top: 8px;
                    font-size: 12px;
                    color: #6b7280;
                    font-weight: 500;
                '>Expires in 5 minutes</div>
            </div>

            <div style='
                padding: 12px 16px;
                background: #fef3c7;
                border-left: 4px solid #f59e0b;
                border-radius: 4px;
                margin-bottom: 24px;
            '>
                <p style='margin:0;font-size:13px;color:#92400e;'>
                    <strong>Security notice:</strong> This code is for one-time use only.
                    Never share it with anyone. Devken CBC staff will never ask for this code.
                </p>
            </div>

            <p style='margin:0;font-size:13px;color:#9ca3af;'>
                If you did not attempt to sign in, please ignore this email.
                Your account remains secure.
            </p>");

        // ── Welcome ───────────────────────────────────────────────────────────

        public static string Welcome(string firstName) => Wrap($@"
            <p style='margin:0 0 16px;font-size:15px;color:#374151;'>
                Hi <strong>{Esc(firstName)}</strong>,
            </p>
            <p style='margin:0 0 16px;font-size:15px;color:#374151;line-height:1.6;'>
                Welcome to <strong>Devken CBC School Management System</strong>!
                Your account has been set up and is ready to use.
            </p>
            <p style='margin:0 0 24px;font-size:15px;color:#374151;line-height:1.6;'>
                You can sign in using your Google account or your email and password
                at any time.
            </p>
            <div style='text-align:center;margin-bottom:24px;'>
                <a href='https://app.devkencbc.com/sign-in'
                   style='
                       display:inline-block;
                       padding:12px 32px;
                       background:linear-gradient(135deg,#1e3a8a,#3b82f6);
                       color:#fff;
                       text-decoration:none;
                       border-radius:8px;
                       font-weight:600;
                       font-size:15px;
                   '>
                    Sign in to Devken CBC
                </a>
            </div>
            <p style='margin:0;font-size:13px;color:#9ca3af;'>
                If you did not create this account, please contact your school administrator.
            </p>");

        // ── Password reset (bonus — useful to have) ───────────────────────────

        public static string PasswordReset(string firstName, string resetLink) => Wrap($@"
            <p style='margin:0 0 16px;font-size:15px;color:#374151;'>
                Hi <strong>{Esc(firstName)}</strong>,
            </p>
            <p style='margin:0 0 24px;font-size:15px;color:#374151;line-height:1.6;'>
                We received a request to reset the password for your Devken CBC account.
                Click the button below to choose a new password.
            </p>
            <div style='text-align:center;margin-bottom:24px;'>
                <a href='{resetLink}'
                   style='
                       display:inline-block;
                       padding:12px 32px;
                       background:linear-gradient(135deg,#1e3a8a,#3b82f6);
                       color:#fff;
                       text-decoration:none;
                       border-radius:8px;
                       font-weight:600;
                       font-size:15px;
                   '>
                    Reset my password
                </a>
            </div>
            <p style='margin:0 0 16px;font-size:13px;color:#6b7280;'>
                This link expires in <strong>30 minutes</strong>.
                If you did not request a password reset, you can safely ignore this email.
            </p>");

        // ── Shared branded wrapper ────────────────────────────────────────────

        private static string Wrap(string content) => $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width,initial-scale=1.0' />
  <title>Devken CBC</title>
</head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;'>

  <!-- Outer wrapper -->
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:40px 16px;'>
    <tr>
      <td align='center'>

        <!-- Card -->
        <table width='100%' cellpadding='0' cellspacing='0'
               style='max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;
                      box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

          <!-- Header -->
          <tr>
            <td style='background:linear-gradient(135deg,#0f172a 0%,#1e3a8a 60%,#1d4ed8 100%);
                       padding:28px 32px;'>
              <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                  <td>
                    <!-- Logo mark -->
                    <div style='
                        display:inline-flex;
                        align-items:center;
                        justify-content:center;
                        width:40px;height:40px;
                        background:rgba(255,255,255,0.15);
                        border-radius:8px;
                        font-size:14px;font-weight:700;
                        color:#fff;letter-spacing:0.03em;
                        vertical-align:middle;
                    '>DK</div>
                    <span style='
                        vertical-align:middle;
                        margin-left:10px;
                        font-size:18px;font-weight:700;
                        color:#ffffff;letter-spacing:-0.01em;
                    '>Devken CBC</span>
                    <div style='
                        margin-top:2px;margin-left:50px;
                        font-size:11px;color:rgba(255,255,255,0.65);
                        letter-spacing:0.03em;
                    '>School Management System</div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='padding:32px;'>
              {content}
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='
                padding:20px 32px;
                background:#f8fafc;
                border-top:1px solid #e2e8f0;
                text-align:center;
            '>
              <p style='margin:0 0 4px;font-size:12px;color:#94a3b8;'>
                &copy; {DateTime.UtcNow.Year} Devken CBC School Management System.
                All rights reserved.
              </p>
              <p style='margin:0;font-size:12px;color:#94a3b8;'>
                This is an automated message — please do not reply directly.
              </p>
            </td>
          </tr>

        </table>
        <!-- /Card -->

      </td>
    </tr>
  </table>

</body>
</html>";

        /// <summary>HTML-encodes user-supplied strings to prevent injection.</summary>
        private static string Esc(string? value)
            => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
