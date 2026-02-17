using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class User : TenantBaseEntity<Guid>
    {
        // ── Identity ───────────────────────────────────────
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        // ── Profile ────────────────────────────────────────
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }

        // ── Account state ──────────────────────────────────
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;
        public bool RequirePasswordChange { get; set; } = true;

        // ── Lockout ────────────────────────────────────────
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }

        // ── Password reset / email verification tokens ────
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiresAt { get; set; }

        // ── Navigation ─────────────────────────────────────
        public School? Tenant { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // ── Computed helpers ───────────────────────────────
        public string FullName => $"{FirstName} {LastName}".Trim();

        public bool IsLockedOut =>
            LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        public bool IsSuperAdmin { get; set; } = false;
        public Guid? SchoolId { get; set; }
    }
}
