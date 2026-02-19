using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentDto
    {
        // ── Base Assessment fields ──────────────────────────────────────
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AssessmentType { get; set; } = string.Empty;
        public decimal MaximumScore { get; set; }
        public DateTime AssessmentDate { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime CreatedOn { get; set; }

        // ── Relationships ───────────────────────────────────────────────
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public Guid? TermId { get; set; }
        public string? TermName { get; set; }
        public Guid? AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public Guid SchoolId { get; set; }

        // ── Summative-specific fields ───────────────────────────────────
        public string? ExamType { get; set; }           // EndTerm, MidTerm, Final
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; }
        public bool HasPracticalComponent { get; set; }
        public decimal PracticalWeight { get; set; }
        public decimal TheoryWeight { get; set; }
        public string? Instructions { get; set; }

        // ── Scores (optional — included when requested) ─────────────────
        public IEnumerable<SummativeAssessmentScoreDto>? Scores { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateSummativeAssessmentRequest
    {
        // ── Base fields ─────────────────────────────────────────────────
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public decimal MaximumScore { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        public Guid? TeacherId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? TermId { get; set; }
        public Guid? AcademicYearId { get; set; }

        /// <summary>Required when caller is SuperAdmin.</summary>
        public Guid? SchoolId { get; set; }

        // ── Summative-specific ──────────────────────────────────────────
        [MaxLength(50)]
        public string? ExamType { get; set; }

        public TimeSpan? Duration { get; set; }

        public int NumberOfQuestions { get; set; }

        [Range(0, 100)]
        public decimal PassMark { get; set; } = 50.0m;

        public bool HasPracticalComponent { get; set; } = false;

        [Range(0, 100)]
        public decimal PracticalWeight { get; set; } = 0.0m;

        [Range(0, 100)]
        public decimal TheoryWeight { get; set; } = 100.0m;

        [MaxLength(1000)]
        public string? Instructions { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateSummativeAssessmentRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public decimal MaximumScore { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        public Guid? TeacherId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? TermId { get; set; }
        public Guid? AcademicYearId { get; set; }

        [MaxLength(50)]
        public string? ExamType { get; set; }

        public TimeSpan? Duration { get; set; }

        public int NumberOfQuestions { get; set; }

        [Range(0, 100)]
        public decimal PassMark { get; set; }

        public bool HasPracticalComponent { get; set; }

        [Range(0, 100)]
        public decimal PracticalWeight { get; set; }

        [Range(0, 100)]
        public decimal TheoryWeight { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentScoreDto
    {
        public Guid Id { get; set; }
        public Guid? SummativeAssessmentId { get; set; }
        public string? AssessmentTitle { get; set; }

        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }

        public decimal TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal MaximumTheoryScore { get; set; }
        public decimal? MaximumPracticalScore { get; set; }

        // Computed
        public decimal TotalScore { get; set; }
        public decimal MaximumTotalScore { get; set; }
        public decimal Percentage { get; set; }
        public string? PerformanceStatus { get; set; }

        public string? Grade { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }
        public bool IsPassed { get; set; }
        public string? Comments { get; set; }

        public DateTime? GradedDate { get; set; }
        public Guid? GradedById { get; set; }
        public string? GradedByName { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateSummativeAssessmentScoreRequest
    {
        [Required]
        public Guid SummativeAssessmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal TheoryScore { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PracticalScore { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal MaximumTheoryScore { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? MaximumPracticalScore { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? Remarks { get; set; }

        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateSummativeAssessmentScoreRequest
    {
        [Required, Range(0, double.MaxValue)]
        public decimal TheoryScore { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PracticalScore { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal MaximumTheoryScore { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? MaximumPracticalScore { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? Remarks { get; set; }

        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // PUBLISH REQUEST (shared with Formative — reused)
    // ═══════════════════════════════════════════════════════════════════

    // UpdateAssessmentPublishRequest is already defined for Formative;
    // both controllers will reference the same DTO.
}