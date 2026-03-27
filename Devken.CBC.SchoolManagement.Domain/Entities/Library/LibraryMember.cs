using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibraryMember : TenantBaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        public string MemberNumber { get; set; } = string.Empty;

        public LibraryMemberType MemberType { get; set; }

        public DateTime JoinedOn { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<BookBorrow> BorrowTransactions { get; set; } = new List<BookBorrow>();

        [ForeignKey("UserId")] // Explicitly link UserId to User
        public virtual User? User { get; set; }
    }
}
