using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    /// <summary>
    /// Tracks each revision/amendment to a budget for audit trail purposes.
    /// </summary>
    public class BudgetRevision : TenantBaseEntity<Guid>
    {
        public Guid BudgetId { get; set; }
        public int RevisionNumber { get; set; }
        public DateTime RevisionDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        public decimal PreviousTotalRevenue { get; set; }
        public decimal NewTotalRevenue { get; set; }
        public decimal PreviousTotalExpense { get; set; }
        public decimal NewTotalExpense { get; set; }

        public Guid RevisedBy { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Budget Budget { get; set; } = null!;
    }
}
