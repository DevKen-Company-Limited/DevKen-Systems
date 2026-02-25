using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    /// <summary>
    /// Annual or term budget for the school.
    /// Budget lines map to GL accounts for variance analysis.
    /// </summary>
    public class Budget : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!; // e.g. "Annual Budget 2025"

        [MaxLength(500)]
        public string? Description { get; set; }

        // ─── Period ───────────────────────────────────────────────────────────────────

        public Guid AccountingPeriodId { get; set; }
        public int FiscalYear { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // ─── Status ──────────────────────────────────────────────────────────────────

        public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }

        // ─── Totals (denormalized for quick reporting) ────────────────────────────────

        public decimal TotalRevenueBudget { get; set; }
        public decimal TotalExpenseBudget { get; set; }

        [NotMapped]
        public decimal BudgetedSurplusDeficit => TotalRevenueBudget - TotalExpenseBudget;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public AccountingPeriod AccountingPeriod { get; set; } = null!;
        public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
        public ICollection<BudgetRevision> Revisions { get; set; } = new List<BudgetRevision>();

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public void Approve(Guid approvedBy)
        {
            if (Status != BudgetStatus.Draft)
                throw new InvalidOperationException("Only draft budgets can be approved.");

            Status = BudgetStatus.Active;
            ApprovedBy = approvedBy;
            ApprovedOn = DateTime.UtcNow;
        }
    }

}
