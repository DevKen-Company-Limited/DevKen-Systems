using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookBorrowItem : TenantBaseEntity<Guid>
    {
        public Guid BorrowId { get; set; }
        public BookBorrow Borrow { get; set; }

        public Guid BookCopyId { get; set; }
        public BookCopy BookCopy { get; set; }

        public DateTime? ReturnedOn { get; set; }

        public bool IsReturned => ReturnedOn.HasValue;

        public bool IsOverdue { get; set; }

        public ICollection<LibraryFine> Fines { get; set; } = new List<LibraryFine>();
    }
}
