using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
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

        // ── Navigation Properties ─────────────────────────
        public User User { get; set; } = null!;
        public School? Tenant { get; set; }
    }
}
