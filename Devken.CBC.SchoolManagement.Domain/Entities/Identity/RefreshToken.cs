using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class RefreshToken : TenantBaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }    // backing field for IsRevoked
        public Guid? ReplacedByTokenId { get; set; }
        public string? IpAddress { get; set; }
        public User? User { get; set; }
        public RefreshToken? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;  // read-only
        public bool IsActive => !IsExpired && !IsRevoked;

        // ✅ Add a method to revoke the token
        public void Revoke()
        {
            if (!IsRevoked)
                RevokedAt = DateTime.UtcNow;
        }

        // ✅ Optional constructor helper
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

        public RefreshToken() { } // EF Core needs parameterless constructor
    }

}
