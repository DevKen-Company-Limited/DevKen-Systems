using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookPublisher : TenantBaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
