using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ─────────────────────────────────────────────────────────────────────────
    // Response DTO
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Read-only projection of an Assessment1 entity.</summary>
    public class AssessmentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public Guid TeacherId { get; set; }
        public string? TeacherName { get; set; }

        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public Guid ClassId { get; set; }
        public string? ClassName { get; set; }

        public Guid TermId { get; set; }
        public string? TermName { get; set; }

        public Guid AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }

        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public string AssessmentType { get; set; } = null!;  // Formative | Summative | Competency

        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }

        public Guid SchoolId { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Create request
    // ─────────────────────────────────────────────────────────────────────────

    public class CreateAssessmentRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Guid TeacherId { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required]
        public Guid ClassId { get; set; }

        [Required]
        public Guid TermId { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        [Required, Range(0.01, 9999.99)]
        public decimal MaximumScore { get; set; }

        /// <summary>Allowed values: Formative, Summative, Competency</summary>
        [Required, MaxLength(20)]
        public string AssessmentType { get; set; } = null!;

        /// <summary>
        /// Required only when the caller is a SuperAdmin.
        /// Regular users inherit their school from the JWT token.
        /// </summary>
        public Guid? SchoolId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Update request
    // ─────────────────────────────────────────────────────────────────────────

    public class UpdateAssessmentRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Guid TeacherId { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required]
        public Guid ClassId { get; set; }

        [Required]
        public Guid TermId { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        [Required, Range(0.01, 9999.99)]
        public decimal MaximumScore { get; set; }

        [Required, MaxLength(20)]
        public string AssessmentType { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Publish / un-publish request
    // ─────────────────────────────────────────────────────────────────────────

    public class UpdateAssessmentPublishRequest
    {
        /// <summary>True to publish; false to retract.</summary>
        [Required]
        public bool IsPublished { get; set; }
    }
}