using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentDto
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

        // ── Formative-specific fields ───────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? LearningOutcomeName { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; }
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
        public string? Criteria { get; set; }
        public string? Instructions { get; set; }
        public decimal AssessmentWeight { get; set; }

        // ── Scores (optional — included when requested) ─────────────────
        public IEnumerable<FormativeAssessmentScoreDto>? Scores { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateFormativeAssessmentRequest
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

        // ── Formative-specific ──────────────────────────────────────────
        [MaxLength(50)]
        public string? FormativeType { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public Guid? LearningOutcomeId { get; set; }

        [MaxLength(1000)]
        public string? FeedbackTemplate { get; set; }

        public bool RequiresRubric { get; set; } = false;

        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        [MaxLength(500)]
        public string? Criteria { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        public decimal AssessmentWeight { get; set; } = 100.0m;
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateFormativeAssessmentRequest
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
        public string? FormativeType { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public Guid? LearningOutcomeId { get; set; }

        [MaxLength(1000)]
        public string? FeedbackTemplate { get; set; }

        public bool RequiresRubric { get; set; }

        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        [MaxLength(500)]
        public string? Criteria { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        public decimal AssessmentWeight { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentScoreDto
    {
        public Guid Id { get; set; }
        public Guid? FormativeAssessmentId { get; set; }
        public string? AssessmentTitle { get; set; }

        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }

        public decimal Score { get; set; }
        public decimal MaximumScore { get; set; }
        public decimal Percentage { get; set; }

        public string? Grade { get; set; }
        public string? PerformanceLevel { get; set; }
        public string? Feedback { get; set; }
        public string? Strengths { get; set; }
        public string? AreasForImprovement { get; set; }

        public bool IsSubmitted { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public DateTime? GradedDate { get; set; }

        public Guid? GradedById { get; set; }
        public string? GradedByName { get; set; }

        public string? CompetencyArea { get; set; }
        public bool CompetencyAchieved { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateFormativeAssessmentScoreRequest
    {
        [Required]
        public Guid FormativeAssessmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Score { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal MaximumScore { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? PerformanceLevel { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? Strengths { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public bool CompetencyAchieved { get; set; } = false;
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateFormativeAssessmentScoreRequest
    {
        [Required, Range(0, double.MaxValue)]
        public decimal Score { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal MaximumScore { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? PerformanceLevel { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? Strengths { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public bool CompetencyAchieved { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SCORE SUBMISSION — Bulk submit scores for a class
    // ═══════════════════════════════════════════════════════════════════

    public class SubmitFormativeScoreRequest
    {
        [Required]
        public Guid ScoreId { get; set; }

        /// <summary>Set to true to submit, false to retract.</summary>
        public bool IsSubmitted { get; set; } = true;
    }

    public class BulkSubmitFormativeScoresRequest
    {
        [Required, MinLength(1)]
        public List<SubmitFormativeScoreRequest> Scores { get; set; } = new();
    }
}