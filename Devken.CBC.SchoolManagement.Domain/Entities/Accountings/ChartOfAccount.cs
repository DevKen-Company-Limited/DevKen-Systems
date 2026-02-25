using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accounting
{
    /// <summary>
    /// Represents a General Ledger (GL) account in the school's chart of accounts.
    /// Follows double-entry bookkeeping: Assets & Expenses increase with Debit;
    /// Liabilities, Equity & Revenue increase with Credit.
    /// </summary>
    public class ChartOfAccount : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Unique GL account code. Convention: 1xxx=Asset, 2xxx=Liability,
        /// 3xxx=Equity, 4xxx=Revenue, 5xxx=Expense.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string AccountCode { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string AccountName { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        // ─── Classification ──────────────────────────────────────────────────────────

        public AccountType AccountType { get; set; }
        public AccountSubType AccountSubType { get; set; }

        /// <summary>Which side increases this account: Debit or Credit.</summary>
        public DebitCredit NormalBalance { get; set; }

        // ─── Hierarchy (supports parent-child GL grouping) ───────────────────────────

        public Guid? ParentAccountId { get; set; }
        public bool IsHeader { get; set; } = false; // Header = group/summary, not postable

        // ─── Control ─────────────────────────────────────────────────────────────────

        public bool IsActive { get; set; } = true;
        public bool AllowDirectPosting { get; set; } = true;
        public bool IsSystemAccount { get; set; } = false; // Reserved for auto-posting

        /// <summary>
        /// Current running balance. Updated on each journal post.
        /// For reporting snapshots — always recalculate from journal lines for audits.
        /// </summary>
        public decimal CurrentBalance { get; set; } = 0.0m;

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ChartOfAccount? ParentAccount { get; set; }
        public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
        public ICollection<JournalEntryLine> JournalLines { get; set; } = new List<JournalEntryLine>();
        public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();

        // ─── Computed ────────────────────────────────────────────────────────────────

        public string DisplayCode => $"{AccountCode} - {AccountName}";
    }
}