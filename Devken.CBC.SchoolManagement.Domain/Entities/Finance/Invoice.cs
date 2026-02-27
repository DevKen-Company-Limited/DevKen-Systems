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
        // ─── Identity ─────────────────────────────────────────────

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; private set; } = null!;

        // ─── Foreign Keys ──────────────────────────────────────────

        public Guid StudentId { get; private set; }
        public Guid AcademicYearId { get; private set; }
        public Guid? TermId { get; private set; }
        public Guid? ParentId { get; private set; }

        // ─── Dates ─────────────────────────────────────────────────

        [Required]
        public DateTime InvoiceDate { get; private set; }

        [Required]
        public DateTime DueDate { get; private set; }

        // ─── Financials ────────────────────────────────────────────

        [MaxLength(500)]
        public string? Description { get; private set; }

        public decimal TotalAmount { get; private set; }

        public decimal DiscountAmount { get; private set; } = 0m;

        // 🔥 REMOVE stored AmountPaid (computed instead)
        [NotMapped]
        public decimal AmountPaid => Payments.Sum(p => p.Amount);

        [NotMapped]
        public decimal Balance => TotalAmount - DiscountAmount - AmountPaid;

        // ─── Status ────────────────────────────────────────────────

        public InvoiceStatus StatusInvoice { get; private set; } = InvoiceStatus.Pending;

        [NotMapped]
        public bool IsOverdue =>
            DateTime.Today > DueDate &&
            StatusInvoice is InvoiceStatus.Pending or InvoiceStatus.PartiallyPaid;

        // ─── Meta ─────────────────────────────────────────────────

        [MaxLength(1000)]
        public string? Notes { get; private set; }

        // ─── Navigation ───────────────────────────────────────────

        public Student Student { get; private set; } = null!;
        public AcademicYear AcademicYear { get; private set; } = null!;
        public Term? Term { get; private set; }
        public Parent? Parent { get; private set; }

        public ICollection<InvoiceItem> Items { get; private set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
        public ICollection<CreditNote> CreditNotes { get; private set; } = new List<CreditNote>();

        public PaymentPlan? PaymentPlan { get; private set; }

        // ───────────────────────────────────────────────────────────
        // DOMAIN METHODS (SAFE)
        // ───────────────────────────────────────────────────────────

        public void RecalculateTotals()
        {
            if (!Items.Any())
                throw new InvalidOperationException("Invoice must contain at least one item.");

            TotalAmount = Items.Sum(i => i.NetAmount);

            if (TotalAmount < 0)
                throw new InvalidOperationException("Total amount cannot be negative.");

            UpdateStatus();
        }

        public void ApplyPayment(Payment payment)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            if (payment.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive.");

            if (payment.Amount > Balance)
                throw new InvalidOperationException("Payment exceeds outstanding balance.");

            Payments.Add(payment);

            UpdateStatus();
        }

        public void ApplyCredit(decimal creditAmount)
        {
            if (creditAmount <= 0)
                throw new InvalidOperationException("Credit must be positive.");

            if (creditAmount > Balance)
                throw new InvalidOperationException("Credit exceeds outstanding balance.");

            DiscountAmount += creditAmount;

            UpdateStatus();
        }

        public void Cancel()
        {
            if (StatusInvoice == InvoiceStatus.Paid)
                throw new InvalidOperationException("Cannot cancel a paid invoice.");

            StatusInvoice = InvoiceStatus.Cancelled;
        }

        private void UpdateStatus()
        {
            if (StatusInvoice is InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
                return;

            if (Balance <= 0)
            {
                StatusInvoice = InvoiceStatus.Paid;
                return;
            }

            if (AmountPaid > 0)
            {
                StatusInvoice = DateTime.Today > DueDate
                    ? InvoiceStatus.Overdue
                    : InvoiceStatus.PartiallyPaid;

                return;
            }

            StatusInvoice = DateTime.Today > DueDate
                ? InvoiceStatus.Overdue
                : InvoiceStatus.Pending;
        }
    }
}