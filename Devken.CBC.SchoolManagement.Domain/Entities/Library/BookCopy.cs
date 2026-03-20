using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookCopy : TenantBaseEntity<Guid>
    {
        public Guid BookId { get; set; }
        public Book Book { get; set; } 

        public Guid LibraryBranchId { get; set; }
        public LibraryBranch LibraryBranch { get; set; }

        public string AccessionNumber { get; set; } = string.Empty;

        public string Barcode { get; set; } = string.Empty;

        public string? QRCode { get; set; }

        public BookCondition Condition { get; set; } = BookCondition.Good;

        public bool IsAvailable { get; set; } = true;

        public bool IsLost { get; set; }

        public bool IsDamaged { get; set; }

        public DateTime? AcquiredOn { get; set; }
    }
}
