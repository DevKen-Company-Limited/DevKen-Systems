using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Administration
{
    public class UserActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string ActivityType { get; set; } = string.Empty; // e.g., "Login", "Logout", "PasswordChange"
        public string ActivityDetails { get; set; } = string.Empty; // optional extra info
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
