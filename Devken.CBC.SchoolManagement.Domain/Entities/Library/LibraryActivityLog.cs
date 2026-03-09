using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibraryActivityLog : TenantBaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string? Description { get; set; }
    }
}
