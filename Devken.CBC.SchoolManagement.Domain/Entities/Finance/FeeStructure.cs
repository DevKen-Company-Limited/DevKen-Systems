using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Finance
{
    /// <summary>
    /// Defines the actual fee amount for a <see cref="FeeItem"/> in a given
    /// academic year, term, CBC level, and student category.
    /// This is CRITICAL for CBC where PP1 vs Grade 9 vs Grade 12 all pay different amounts.
    /// </summary>
    public class FeeStructure : TenantBaseEntity<Guid>
    {
        // ─── Foreign Keys ────────────────────────────────────────────────────────────

        public Guid FeeItemId { get; set; }
        public Guid AcademicYearId { get; set; }

        /// <summary>Null = annual fee (not term-specific).</summary>
        public Guid? TermId { get; set; }

        // ─── CBC Targeting ───────────────────────────────────────────────────────────

        /// <summary>
        /// Null = applies to ALL levels.
        /// Set to a specific CBCLevel (e.g. PP1, Grade7) to target that level.
        /// </summary>
        public CBCLevel? Level { get; set; }

        /// <summary>All, Day, Boarding, Special.</summary>
        public ApplicableTo ApplicableTo { get; set; } = ApplicableTo.All;

        // ─── Amount ──────────────────────────────────────────────────────────────────

        public decimal Amount { get; set; }

        /// <summary>
        /// Optional: maximum discount percentage allowed on this fee.
        /// Enforced in application layer during discount application.
        /// </summary>
        public decimal? MaxDiscountPercent { get; set; }

        // ─── Validity ────────────────────────────────────────────────────────────────

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        // ─── Navigation ──────────────────────────────────────────────────────────────

        public FeeItem FeeItem { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public Term? Term { get; set; }
    }
}