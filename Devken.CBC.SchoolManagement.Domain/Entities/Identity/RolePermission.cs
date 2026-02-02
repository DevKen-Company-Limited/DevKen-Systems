using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class RolePermission : BaseEntity<Guid>
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public Role? Role { get; set; }
        public Permission? Permission { get; set; }
    }
}
