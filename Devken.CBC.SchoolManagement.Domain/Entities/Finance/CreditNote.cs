using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    /// <summary>
    /// Issued when a student overpays or when a fee is waived/refunded.
    /// Can be applied against future invoices.
    /// </summary>
    public class CreditNote : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string CreditNoteNumber { get; set; } = null!;

        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid StudentId { get; set; }

        /// <summary>Original invoice that triggered this credit note.</summary>
        public Guid InvoiceId { get; set; }

        /// <summary>Invoice this credit note was applied against (null until applied).</summary>
        public Guid? AppliedToInvoiceId { get; set; }

        public Guid? IssuedBy { get; set; }  // FK → Staff.Id

        // ─── Financials ──────────────────────────────────────────────────────────────

        public decimal Amount { get; set; }
        public decimal AmountApplied { get; set; } = 0.0m;

        public decimal RemainingBalance => Amount - AmountApplied;

        // ─── Details ─────────────────────────────────────────────────────────────────

        public DateTime IssuedDate { get; set; }
        public DateTime? AppliedDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Issued;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Student Student { get; set; } = null!;
        public Invoice Invoice { get; set; } = null!;
        public Invoice? AppliedToInvoice { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public void Apply(Guid targetInvoiceId, decimal amount)
        {
            if (amount > RemainingBalance)
                throw new InvalidOperationException("Cannot apply more than remaining credit balance.");

            AmountApplied += amount;
            AppliedToInvoiceId = targetInvoiceId;
            AppliedDate = DateTime.UtcNow;
            Status = AmountApplied >= Amount ? CreditNoteStatus.Applied : Status;
        }

        public void Void()
        {
            if (Status == CreditNoteStatus.Applied)
                throw new InvalidOperationException("Cannot void a fully applied credit note.");

            Status = CreditNoteStatus.Voided;
        }
    }
}