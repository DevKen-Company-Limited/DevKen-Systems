using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Finance
{
    // ─────────────────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Payload for creating a new FeeStructure.
    /// TenantId is set automatically from the JWT for non-SuperAdmin users.
    /// </summary>
    public class CreateFeeStructureDto
    {
        /// <summary>
        /// Set by the controller for non-SuperAdmin users.
        /// SuperAdmin must supply this explicitly.
        /// </summary>
        public Guid? TenantId { get; set; }

        [Required(ErrorMessage = "FeeItemId is required.")]
        public Guid FeeItemId { get; set; }

        [Required(ErrorMessage = "AcademicYearId is required.")]
        public Guid AcademicYearId { get; set; }

        /// <summary>Null = annual fee (not term-specific).</summary>
        public Guid? TermId { get; set; }

        /// <summary>Null = applies to all CBC levels.</summary>
        public CBCLevel? Level { get; set; }

        /// <summary>Student category this fee applies to. Default: All.</summary>
        public ApplicableTo ApplicableTo { get; set; } = ApplicableTo.All;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        /// <summary>Optional maximum discount percentage (0–100).</summary>
        [Range(0, 100, ErrorMessage = "MaxDiscountPercent must be between 0 and 100.")]
        public decimal? MaxDiscountPercent { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Payload for updating an existing FeeStructure.
    /// FeeItemId, AcademicYearId, and TermId are not updatable after creation
    /// to preserve financial integrity; delete and recreate instead.
    /// </summary>
    public class UpdateFeeStructureDto
    {
        /// <summary>Null = applies to all CBC levels.</summary>
        public CBCLevel? Level { get; set; }

        public ApplicableTo ApplicableTo { get; set; } = ApplicableTo.All;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Range(0, 100, ErrorMessage = "MaxDiscountPercent must be between 0 and 100.")]
        public decimal? MaxDiscountPercent { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESPONSE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// API response shape for a FeeStructure, flattening navigation properties
    /// for convenience.
    /// </summary>
    public class FeeStructureDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        // Foreign keys
        public Guid FeeItemId { get; set; }
        public string FeeItemName { get; set; } = string.Empty;
        public Guid AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }

        // CBC Targeting
        public CBCLevel? Level { get; set; }
        public ApplicableTo ApplicableTo { get; set; }

        // Amounts
        public decimal Amount { get; set; }
        public decimal? MaxDiscountPercent { get; set; }

        // Validity
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }

        // Audit
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}