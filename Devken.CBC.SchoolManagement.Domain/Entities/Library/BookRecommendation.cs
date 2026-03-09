using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookRecommendation : TenantBaseEntity<Guid>
    {
        public Guid BookId { get; set; }

        public Guid StudentId { get; set; }

        public int Score { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
