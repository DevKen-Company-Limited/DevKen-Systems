using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Administration
{
    public class School : BaseEntity<Guid>
    {
        // ── Basic Info ────────────────────────────────────
        public string SlugName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }

        // ── Account state ─────────────────────────────────
        public bool IsActive { get; set; } = true;

        // ── Navigation ───────────────────────────────────
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
