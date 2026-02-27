using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Accountings.Banks
{
    /// <summary>
    /// Records a transfer of funds between two bank/cash accounts.
    /// Generates a balanced journal entry: Debit destination, Credit source.
    /// </summary>
    public class FundTransfer : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string TransferReference { get; set; } = null!;

        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid FromAccountId { get; set; }       // FK → BankAccount.Id
        public Guid ToAccountId { get; set; }         // FK → BankAccount.Id
        public Guid? JournalEntryId { get; set; }     // Auto-created on completion
        public Guid InitiatedBy { get; set; }         // FK → Staff.Id
        public Guid? ApprovedBy { get; set; }

        // ─── Transfer Details ────────────────────────────────────────────────────────

        public decimal Amount { get; set; }
        public DateTime TransferDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = null!;

        [MaxLength(100)]
        public string? TransactionReference { get; set; } // Bank reference number

        public FundTransferStatus Status { get; set; } = FundTransferStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public BankAccount FromAccount { get; set; } = null!;
        public BankAccount ToAccount { get; set; } = null!;
        public JournalEntry? JournalEntry { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public void Complete(Guid approvedBy, string? bankRef = null)
        {
            if (Status != FundTransferStatus.Pending)
                throw new InvalidOperationException("Only pending transfers can be completed.");

            Status = FundTransferStatus.Completed;
            ApprovedBy = approvedBy;
            CompletedDate = DateTime.UtcNow;
            TransactionReference ??= bankRef;
        }

        public void Reverse()
        {
            if (Status != FundTransferStatus.Completed)
                throw new InvalidOperationException("Only completed transfers can be reversed.");

            Status = FundTransferStatus.Reversed;
        }
    }
}
