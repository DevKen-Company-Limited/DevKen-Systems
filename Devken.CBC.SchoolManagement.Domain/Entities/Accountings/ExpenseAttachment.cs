using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    public class ExpenseAttachment : TenantBaseEntity<Guid>
    {
        public Guid ExpenseId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [MaxLength(50)]
        public string? FileType { get; set; }

        public long? FileSizeBytes { get; set; }

        public Expense Expense { get; set; } = null!;
    }
}
