using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    /// <summary>
    /// Allows an invoice to be split into multiple installment payments.
    /// </summary>
    public class PaymentPlan : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid InvoiceId { get; set; }
        public Guid StudentId { get; set; }
        public Guid? ApprovedBy { get; set; } // FK → Staff.Id

        // ─── Plan Details ────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(100)]
        public string PlanName { get; set; } = null!;

        public int NumberOfInstallments { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Invoice Invoice { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public ICollection<PaymentPlanInstallment> Installments { get; set; } = new List<PaymentPlanInstallment>();

        // ─── Computed ────────────────────────────────────────────────────────────────

        public decimal TotalScheduled => Installments.Sum(i => i.AmountDue);
        public decimal TotalPaid => Installments.Sum(i => i.AmountPaid);
        public int PendingInstallments => Installments.Count(i => i.Status != InstallmentStatus.Paid && i.Status != InstallmentStatus.Waived);
    }

    public class PaymentPlanInstallment : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid PaymentPlanId { get; set; }

        /// <summary>
        /// Linked to the actual payment once the installment is settled.
        /// </summary>
        public Guid? PaymentId { get; set; }

        // ─── Installment Details ─────────────────────────────────────────────────────

        public int InstallmentNumber { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; } = 0.0m;
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public PaymentPlan PaymentPlan { get; set; } = null!;
        public Payment? Payment { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        public void RecordPayment(decimal amount, Guid paymentId)
        {
            AmountPaid += amount;
            PaymentId = paymentId;
            PaidDate = DateTime.UtcNow;

            Status = AmountPaid switch
            {
                0 => DateTime.Today > DueDate ? InstallmentStatus.Overdue : InstallmentStatus.Pending,
                var paid when paid >= AmountDue => InstallmentStatus.Paid,
                _ => InstallmentStatus.PartiallyPaid
            };
        }

        public void Waive(string? reason = null)
        {
            Status = InstallmentStatus.Waived;
            Notes = reason ?? Notes;
        }
    }
}