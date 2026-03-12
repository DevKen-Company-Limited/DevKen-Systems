using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibraryBranch : TenantBaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }

        public ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();
    }
}
