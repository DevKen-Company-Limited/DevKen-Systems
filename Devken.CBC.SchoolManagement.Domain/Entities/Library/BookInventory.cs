using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookInventory : TenantBaseEntity<Guid>
    {
        public Guid BookId { get; set; }

        public Book Book { get; set; }

        public int TotalCopies { get; set; }

        public int AvailableCopies { get; set; }

        public int BorrowedCopies { get; set; }

        public int LostCopies { get; set; }

        public int DamagedCopies { get; set; }
    }
}
