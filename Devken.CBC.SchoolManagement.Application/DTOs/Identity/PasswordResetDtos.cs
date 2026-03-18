using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Identity
{
    namespace Devken.CBC.SchoolManagement.Application.DTOs.Identity
    {
        /// <summary>Submitted by the client to trigger a password-reset email.</summary>
        public record ForgotPasswordRequest(
            [Required, EmailAddress] string Email);

        /// <summary>Submitted by the client to set a new password using the reset token.</summary>
        public record ResetPasswordRequest(
            [Required] string Token,
            [Required, MinLength(8)] string NewPassword);
    }

}
