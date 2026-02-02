using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Administration
{
    public class SuperAdminRefreshToken : TenantBaseEntity<Guid>
    {
        public Guid SuperAdminId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }    // backing field for IsRevoked
        public string? IpAddress { get; set; }

        public SuperAdmin SuperAdmin { get; set; } = null!;

        // ✅ Computed properties
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsActive => !IsExpired && !IsRevoked;

        // ✅ Revoke helper
        public void Revoke()
        {
            if (!IsRevoked)
                RevokedAt = DateTime.UtcNow;
        }

        // ✅ Optional constructor helper
        public SuperAdminRefreshToken(Guid superAdminId, string token, DateTime expiresAt, string? ipAddress = null)
        {
            Id = Guid.NewGuid();
            SuperAdminId = superAdminId;
            Token = token;
            ExpiresAt = expiresAt;
            IpAddress = ipAddress;
            CreatedOn = DateTime.UtcNow;
            UpdatedOn = DateTime.UtcNow;
        }

        public SuperAdminRefreshToken() { } // EF Core needs parameterless constructor
    }
}
