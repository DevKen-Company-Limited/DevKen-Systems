using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class Role : TenantBaseEntity<Guid>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } = false;
        public School? Tenant { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public Guid? SchoolId { get; set; }
    }
}
