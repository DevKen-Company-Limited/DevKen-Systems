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
    /// A single line in a budget, linked to a GL account.
    /// Actual vs budget variance is computed by joining to journal lines.
    /// </summary>
    public class BudgetLine : TenantBaseEntity<Guid>
    {
        public Guid BudgetId { get; set; }
        public Guid AccountId { get; set; } // FK → ChartOfAccount.Id

        [MaxLength(200)]
        public string? Description { get; set; }

        public AccountType AccountType { get; set; }

        /// <summary>Planned amount for the period.</summary>
        public decimal BudgetedAmount { get; set; }

        /// <summary>
        /// Actual spend/revenue — populated by aggregating journal lines.
        /// Not stored; computed during reporting.
        /// </summary>
        [NotMapped]
        public decimal ActualAmount { get; set; }

        [NotMapped]
        public decimal Variance => BudgetedAmount - ActualAmount;

        [NotMapped]
        public decimal VariancePercent => BudgetedAmount != 0
            ? (Variance / BudgetedAmount) * 100
            : 0;

        [MaxLength(100)]
        public string? CostCentre { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Budget Budget { get; set; } = null!;
        public ChartOfAccount Account { get; set; } = null!;
    }

}
