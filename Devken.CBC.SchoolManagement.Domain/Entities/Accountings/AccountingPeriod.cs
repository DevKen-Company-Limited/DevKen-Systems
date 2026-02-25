using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accounting
{
    /// <summary>
    /// Represents a financial reporting period (month/term/year).
    /// Closing a period prevents new postings to it — critical for audit integrity.
    /// </summary>
    public class AccountingPeriod : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!; // e.g. "Term 1 2025", "January 2025"

        public int FiscalYear { get; set; }      // e.g. 2025
        public int PeriodNumber { get; set; }    // 1–12 (monthly) or 1–3 (termly)

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

        public DateTime? ClosedOn { get; set; }
        public Guid? ClosedBy { get; set; } // FK → Staff.Id

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public bool IsOpen => Status == AccountingPeriodStatus.Open;

        public void Close(Guid closedBy)
        {
            if (Status == AccountingPeriodStatus.Locked)
                throw new InvalidOperationException("Period is locked and cannot be modified.");

            Status = AccountingPeriodStatus.Closed;
            ClosedOn = DateTime.UtcNow;
            ClosedBy = closedBy;
        }

        public void Lock()
        {
            Status = AccountingPeriodStatus.Locked;
        }

        public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
    }
}