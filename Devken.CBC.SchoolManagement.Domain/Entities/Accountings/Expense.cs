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
    /// Records a school expenditure. Triggers a journal entry on approval.
    /// </summary>
    public class Expense : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string ExpenseNumber { get; set; } = null!;

        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid CategoryId { get; set; }
        public Guid? GlAccountId { get; set; }        // Can override category's account
        public Guid? AccountingPeriodId { get; set; }
        public Guid? VendorId { get; set; }           // FK → Vendor (if vendor module exists)
        public Guid? JournalEntryId { get; set; }     // Auto-created on approval

        // ─── Personnel ───────────────────────────────────────────────────────────────

        public Guid RequestedBy { get; set; }          // FK → Staff.Id
        public Guid? ApprovedBy { get; set; }
        public Guid? PaidBy { get; set; }

        // ─── Details ─────────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = null!;

        public DateTime ExpenseDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; } = 0.0m;
        public decimal TotalAmount => Amount + TaxAmount;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

        [MaxLength(50)]
        public string? ReceiptNumber { get; set; }

        [MaxLength(100)]
        public string? CostCentre { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ExpenseCategory Category { get; set; } = null!;
        public ChartOfAccount? GlAccount { get; set; }
        public JournalEntry? JournalEntry { get; set; }
        public ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public void Approve(Guid approvedBy)
        {
            if (Status != ExpenseStatus.Submitted)
                throw new InvalidOperationException("Only submitted expenses can be approved.");

            Status = ExpenseStatus.Approved;
            ApprovedBy = approvedBy;
            ApprovedDate = DateTime.UtcNow;
        }

        public void Reject(Guid rejectedBy, string reason)
        {
            Status = ExpenseStatus.Rejected;
            Notes = $"Rejected by {rejectedBy}: {reason}";
        }

        public void MarkPaid(Guid paidBy)
        {
            if (Status != ExpenseStatus.Approved)
                throw new InvalidOperationException("Only approved expenses can be marked as paid.");

            Status = ExpenseStatus.Paid;
            PaidBy = paidBy;
            PaidDate = DateTime.UtcNow;
        }
    }

}
