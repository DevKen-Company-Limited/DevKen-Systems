using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.NumberSeries
{
    public class DocumentNumberSeries : TenantBaseEntity<Guid>
    {
        public string EntityName { get; set; } = default!;
        public string? Prefix { get; set; } = default!;
        public int Padding { get; set; } = 5;

        public int LastNumber { get; set; }
        public bool ResetEveryYear { get; set; }
        public int? LastGeneratedYear { get; set; }
        public School? Tenant { get; set; }
        public string? Description { get; set; }
    }

}
