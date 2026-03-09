using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookReservation : TenantBaseEntity<Guid>
    {
        public Guid BookId { get; set; }
        public Book Book { get; set; }

        public Guid MemberId { get; set; }
        public LibraryMember Member { get; set; }

        public DateTime ReservedOn { get; set; }

        public bool IsFulfilled { get; set; }
    }
}
