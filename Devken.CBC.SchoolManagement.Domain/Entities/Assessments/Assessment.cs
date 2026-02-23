// Devken.CBC.SchoolManagement.Domain/Entities/Assessments/Assessment1.cs
using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    /// <summary>
    /// Base class for all CBC assessment types.
    /// Uses Table-Per-Type (TPT) inheritance — each subclass gets its own table.
    /// Shared columns land in "Assessments"; type-specific columns in their own tables.
    /// </summary>
    public abstract class Assessment1 : TenantBaseEntity<Guid>
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        // ── Foreign Keys ──────────────────────────────────────────────────────
        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid TermId { get; set; }
        public Term Term { get; set; } = null!;

        public Guid AcademicYearId { get; set; }
        public AcademicYear AcademicYear { get; set; } = null!;

        // ── Core Fields ───────────────────────────────────────────────────────
        public DateTime AssessmentDate { get; set; }

        [Range(0.01, 9999.99)]
        public decimal MaximumScore { get; set; }

        /// <summary>Discriminator: Formative | Summative | Competency</summary>
        [Required, MaxLength(20)]
        public string AssessmentType { get; set; } = null!;

        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedDate { get; set; }
    }
}