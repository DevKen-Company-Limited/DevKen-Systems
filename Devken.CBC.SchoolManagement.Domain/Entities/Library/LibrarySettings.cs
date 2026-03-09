using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class LibrarySettings : TenantBaseEntity<Guid>
    {
        public int MaxBooksPerStudent { get; set; } = 2;

        public int MaxBooksPerTeacher { get; set; } = 5;

        public int BorrowDaysStudent { get; set; } = 7;

        public int BorrowDaysTeacher { get; set; } = 14;

        public decimal FinePerDay { get; set; } = 10;

        public bool AllowBookReservation { get; set; } = true;
    }
}
