using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class Book : TenantBaseEntity<Guid>
    {
        public string Title { get; set; } = string.Empty;

        public string ISBN { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }
        public BookCategory Category { get; set; }

        public Guid AuthorId { get; set; }
        public BookAuthor Author { get; set; }

        public Guid PublisherId { get; set; }
        public BookPublisher Publisher { get; set; }

        public int PublicationYear { get; set; }

        public string? Language { get; set; }

        public string? Description { get; set; }

        public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    }
}
