using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookBorrow : TenantBaseEntity<Guid>
    {
        public Guid MemberId { get; set; }
        public LibraryMember Member { get; set; }

        public DateTime BorrowDate { get; set; }

        public DateTime DueDate { get; set; }

        public BorrowStatus BStatus { get; set; } = BorrowStatus.Borrowed;

        public ICollection<BookBorrowItem> Items { get; set; } = new List<BookBorrowItem>();
    }
}
