using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    public class Invoice : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = null!;

        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid StudentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid? TermId { get; set; }
        public Guid? ParentId { get; set; }

        // ─── Dates ───────────────────────────────────────────────────────────────────

        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }

        // ─── Financials ──────────────────────────────────────────────────────────────

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; } = 0.0m;
        public decimal AmountPaid { get; set; } = 0.0m;

        /// <summary>Outstanding balance — not stored; computed at runtime.</summary>
        [NotMapped]
        public decimal Balance => TotalAmount - DiscountAmount - AmountPaid;

        // ─── Status ──────────────────────────────────────────────────────────────────

        public InvoiceStatus StatusInvoice { get; set; } = InvoiceStatus.Pending;

        [NotMapped]
        public bool IsOverdue => DateTime.Today > DueDate && StatusInvoice == InvoiceStatus.Pending;

        // ─── Meta ────────────────────────────────────────────────────────────────────

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Student Student { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public Term? Term { get; set; }
        public Parent? Parent { get; set; }
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
        public PaymentPlan? PaymentPlan { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        /// <summary>
        /// Recalculates TotalAmount from line items. Call after adding/removing items.
        /// </summary>
        public void RecalculateTotals()
        {
            TotalAmount = Items.Sum(i => i.NetAmount);
        }

        /// <summary>
        /// Records a payment and updates status accordingly.
        /// Always call this instead of setting AmountPaid directly.
        /// </summary>
        public void ApplyPayment(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive.");

            AmountPaid += amount;
            UpdateStatus();
        }

        /// <summary>
        /// Applies a credit note to this invoice.
        /// </summary>
        public void ApplyCreditNote(decimal creditAmount)
        {
            if (creditAmount <= 0)
                throw new InvalidOperationException("Credit amount must be positive.");

            DiscountAmount += creditAmount;
            UpdateStatus();
        }

        /// <summary>
        /// Recomputes the invoice status from current financial values.
        /// </summary>
        /// public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
        public void UpdateStatus()
        {
            if (StatusInvoice == InvoiceStatus.Cancelled || StatusInvoice == InvoiceStatus.Refunded)
                return; // Do not auto-change terminal states.

            var effective = TotalAmount - DiscountAmount;

            StatusInvoice = AmountPaid switch
            {
                0 => DateTime.Today > DueDate ? InvoiceStatus.Overdue : InvoiceStatus.Pending,
                var paid when paid >= effective => InvoiceStatus.Paid,
                _ => DateTime.Today > DueDate ? InvoiceStatus.Overdue : InvoiceStatus.PartiallyPaid
            };
        }
    }
}