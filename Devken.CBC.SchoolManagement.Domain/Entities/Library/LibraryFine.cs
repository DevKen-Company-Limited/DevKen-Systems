using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibraryFine : TenantBaseEntity<Guid>
    {
        public Guid BorrowItemId { get; set; }

        public BookBorrowItem? BorrowItem { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }

        public DateTime IssuedOn { get; set; }

        public DateTime? PaidOn { get; set; }

        public string? Reason { get; set; }
    }
}
