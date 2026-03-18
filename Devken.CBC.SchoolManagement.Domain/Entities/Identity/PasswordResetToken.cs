using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    /// <summary>
    /// Stores the SHA-256 hash of a one-time password-reset token.
    /// The raw token is only ever held in memory and sent to the user by email —
    /// it is never persisted.
    /// </summary>
    public class PasswordResetToken
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }
        public User? User { get; init; }

        /// <summary>SHA-256 hex digest of the raw token.</summary>
        public string TokenHash { get; init; } = default!;

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }
}
