using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Accounting;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings
{
    /// <summary>
    /// A double-entry bookkeeping journal entry.
    /// Rule: Sum of all Debit lines MUST equal sum of all Credit lines.
    /// </summary>
    public class JournalEntry : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string JournalNumber { get; set; } = null!;

        // ─── Classification ──────────────────────────────────────────────────────────

        public JournalEntryType EntryType { get; set; } = JournalEntryType.Manual;
        public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

        // ─── Dates ───────────────────────────────────────────────────────────────────

        public DateTime EntryDate { get; set; }
        public DateTime? PostedDate { get; set; }
        public Guid AccountingPeriodId { get; set; }

        // ─── Description ─────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = null!;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Source Reference (links back to triggering document) ─────────────────────

        /// <summary>e.g. "Invoice", "Payment", "CreditNote"</summary>
        [MaxLength(50)]
        public string? SourceType { get; set; }

        /// <summary>The ID of the source document (Invoice.Id, Payment.Id, etc.).</summary>
        public Guid? SourceId { get; set; }

        // ─── Reversal Support ────────────────────────────────────────────────────────

        public Guid? ReversesJournalId { get; set; } // Points to original entry
        public bool IsReversal { get; set; } = false;

        // ─── Audit ───────────────────────────────────────────────────────────────────

        public Guid? PreparedBy { get; set; }   // FK → Staff.Id
        public Guid? ApprovedBy { get; set; }   // FK → Staff.Id
        public Guid? PostedBy { get; set; }     // FK → Staff.Id

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public AccountingPeriod AccountingPeriod { get; set; } = null!;
        public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
        public JournalEntry? ReversesJournal { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        /// <summary>
        /// Validates the golden rule: total debits must equal total credits.
        /// Call this before posting.
        /// </summary>
        public bool IsBalanced()
        {
            var totalDebits = Lines.Where(l => l.Side == DebitCredit.Debit).Sum(l => l.Amount);
            var totalCredits = Lines.Where(l => l.Side == DebitCredit.Credit).Sum(l => l.Amount);
            return totalDebits == totalCredits && totalDebits > 0;
        }

        public void Post(Guid postedBy, AccountingPeriod period)
        {
            if (!IsBalanced())
                throw new InvalidOperationException("Journal entry is not balanced. Debits must equal credits.");

            if (!period.IsOpen)
                throw new InvalidOperationException($"Accounting period '{period.Name}' is not open for posting.");

            Status = JournalEntryStatus.Posted;
            PostedDate = DateTime.UtcNow;
            PostedBy = postedBy;
        }

        public decimal TotalDebits => Lines.Where(l => l.Side == DebitCredit.Debit).Sum(l => l.Amount);
        public decimal TotalCredits => Lines.Where(l => l.Side == DebitCredit.Credit).Sum(l => l.Amount);
    }
}
