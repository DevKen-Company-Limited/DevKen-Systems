using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    /// <summary>
    /// Represents a reusable fee definition (the "template").
    /// Actual amounts per year/level/term are defined in <see cref="FeeStructure"/>.
    /// </summary>
    public class FeeItem : TenantBaseEntity<Guid>
    {
        // ─── Identity ───────────────────────────────────────────────────────────────

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        // ─── Classification ──────────────────────────────────────────────────────────

        /// <summary>Stored as int; maps to <see cref="FeeType"/> enum.</summary>
        public FeeType FeeType { get; set; } = FeeType.Tuition;

        public bool IsMandatory { get; set; } = true;
        public bool IsRecurring { get; set; } = false;
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        // ─── Pricing ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fallback amount when no <see cref="FeeStructure"/> matches.
        /// Always define a FeeStructure for production use.
        /// </summary>
        public decimal DefaultAmount { get; set; }

        public bool IsTaxable { get; set; } = false;

        /// <summary>Tax rate in percentage, e.g. 16 for 16% VAT.</summary>
        public decimal? TaxRate { get; set; }

        // ─── General Ledger ──────────────────────────────────────────────────────────

        [MaxLength(100)]
        public string? GlCode { get; set; }

        // ─── CBC Applicability ───────────────────────────────────────────────────────

        /// <summary>Null means the fee applies to ALL CBC levels.</summary>
        public CBCLevel? ApplicableLevel { get; set; }

        public ApplicableTo ApplicableTo { get; set; } = ApplicableTo.All;

        // ─── Status ──────────────────────────────────────────────────────────────────

        public bool IsActive { get; set; } = true;

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<StudentDiscount> StudentDiscounts { get; set; } = new List<StudentDiscount>();

        // ─── Computed ────────────────────────────────────────────────────────────────

        public string DisplayName => $"{Code} - {Name}";
    }
}