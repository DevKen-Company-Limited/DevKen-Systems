using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    /// <summary>
    /// Represents a discount or concession granted to a specific student.
    /// Applied during invoice generation.
    /// </summary>
    public class StudentDiscount : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid StudentId { get; set; }
        public Guid AcademicYearId { get; set; }

        /// <summary>
        /// Null = discount applies to all fees.
        /// Specific FeeItemId = discount applies to that fee only.
        /// </summary>
        public Guid? FeeItemId { get; set; }

        // ─── Discount Definition ─────────────────────────────────────────────────────

        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public DiscountReason Reason { get; set; } = DiscountReason.Other;

        [MaxLength(200)]
        public string? ReasonDescription { get; set; }

        /// <summary>
        /// For Percentage: value is 0–100 (e.g. 50 = 50% off).
        /// For FixedAmount: value is the exact KES amount off.
        /// </summary>
        public decimal Value { get; set; }

        // ─── Validity ────────────────────────────────────────────────────────────────

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        // ─── Approval ────────────────────────────────────────────────────────────────

        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public Student Student { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public FeeItem? FeeItem { get; set; }

        // ─── Domain Methods ──────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the discount amount given a fee amount.
        /// </summary>
        public decimal ComputeDiscount(decimal feeAmount)
        {
            return DiscountType == DiscountType.Percentage
                ? feeAmount * (Value / 100)
                : Math.Min(Value, feeAmount); // Never discount more than the fee
        }

        public bool IsCurrentlyActive(DateTime? asOf = null)
        {
            var date = asOf ?? DateTime.Today;
            return IsActive && EffectiveFrom <= date && (!EffectiveTo.HasValue || EffectiveTo >= date);
        }
    }
}