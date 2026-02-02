using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class UserRole : TenantBaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        public User? User { get; set; }
        public Role? Role { get; set; }
    }
}
