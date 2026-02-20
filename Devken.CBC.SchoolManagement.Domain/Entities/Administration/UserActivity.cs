using Devken.CBC.SchoolManagement.Domain.Common;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Administration
{
    public class UserActivity : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string ActivityDetails { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ─────────────────────────────────────
        // NOTE: User navigation intentionally removed.
        // UserId is stored as a plain FK column. User data is loaded
        // separately in the service layer via IUserRepository.GetByIdsAsync.
        // Having a User navigation property with a non-nullable Guid UserId
        // causes EF Core [10622] because User has a global query filter —
        // EF treats User as a "required end" that could be filtered out.
        public School? Tenant { get; set; }
    }
}