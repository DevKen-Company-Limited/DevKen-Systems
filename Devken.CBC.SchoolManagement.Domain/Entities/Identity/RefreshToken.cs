using Devken.CBC.SchoolManagement.Domain.Common;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class RefreshToken : TenantBaseEntity<Guid>
    {
        // ── Properties ───────────────────────────────
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }           // backing field for IsRevoked
        public Guid? ReplacedByTokenId { get; set; }
        public string? IpAddress { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public RefreshToken? ReplacedByToken { get; set; }

        // ── Computed properties (read-only) ─────────
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Do NOT use this property directly in LINQ-to-Entities queries.
        /// Use RevokedAt == null instead for EF Core translation.
        /// </summary>
        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsActive => !IsExpired && !IsRevoked;

        // ── Methods ─────────────────────────────────
        /// <summary>
        /// Revoke this refresh token
        /// </summary>
        public void Revoke()
        {
            if (!IsRevoked)
            {
                RevokedAt = DateTime.UtcNow;
            }
        }

        // ── Constructors ────────────────────────────
        /// <summary>
        /// Helper constructor to create a new refresh token
        /// </summary>
        public RefreshToken(Guid userId, string token, DateTime expiresAt, string? ipAddress = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Token = token;
            ExpiresAt = expiresAt;
            IpAddress = ipAddress;
            CreatedOn = DateTime.UtcNow;
            UpdatedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Parameterless constructor required by EF Core
        /// </summary>
        public RefreshToken() { }
    }
}
