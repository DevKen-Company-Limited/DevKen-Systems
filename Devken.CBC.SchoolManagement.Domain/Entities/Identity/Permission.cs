using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class Permission : BaseEntity<Guid>
    {

        public string Key { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string GroupName { get; set; } = null!;  
        public string? Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

}
