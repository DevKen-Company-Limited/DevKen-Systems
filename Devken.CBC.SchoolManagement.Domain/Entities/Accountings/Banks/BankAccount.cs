using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings.Banks
{
    /// <summary>
    /// Represents a school bank/cash account for reconciliation purposes.
    /// Linked to a GL account in the Chart of Accounts.
    /// </summary>
    public class BankAccount : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(100)]
        public string AccountName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = null!;

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BranchName { get; set; }

        [MaxLength(20)]
        public string? SwiftCode { get; set; }

        // ─── Linking ─────────────────────────────────────────────────────────────────

        public Guid GlAccountId { get; set; } // FK → ChartOfAccount.Id
        public bool IsCashAccount { get; set; } = false; // True for petty cash

        // ─── Balances ────────────────────────────────────────────────────────────────

        public decimal CurrentBalance { get; set; } = 0.0m;
        public decimal? LastReconciledBalance { get; set; }
        public DateTime? LastReconciledDate { get; set; }

        // ─── Status ──────────────────────────────────────────────────────────────────

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ChartOfAccount GlAccount { get; set; } = null!;
        public ICollection<FundTransfer> TransfersFrom { get; set; } = new List<FundTransfer>();
        public ICollection<FundTransfer> TransfersTo { get; set; } = new List<FundTransfer>();
    }

    // ─────────────────────────────────────────────────────────────────────────────────

}
