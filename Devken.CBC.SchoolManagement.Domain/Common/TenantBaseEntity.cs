using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Common
{
    public abstract class TenantBaseEntity<TId> : BaseEntity<TId> where TId : IEquatable<TId>
    {
        public Guid TenantId { get; set; }
    }
}
