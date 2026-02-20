using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentDto
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

        // ── Competency-specific fields ──────────────────────────────────
        public string CompetencyName { get; set; } = string.Empty;
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
        public CBCLevel TargetLevel { get; set; }
        public string? TargetLevelDisplay => TargetLevel.ToString();
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod AssessmentMethod { get; set; }
        public string? AssessmentMethodDisplay => AssessmentMethod.ToString();
        public string? RatingScale { get; set; }
        public bool IsObservationBased { get; set; }
        public string? ToolsRequired { get; set; }
        public string? Instructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }

        // ── Scores (optional — included when requested) ─────────────────
        public IEnumerable<CompetencyAssessmentScoreDto>? Scores { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateCompetencyAssessmentRequest
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

        // ── Competency-specific ─────────────────────────────────────────
        [Required, MaxLength(100)]
        public string CompetencyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        [Required]
        public CBCLevel TargetLevel { get; set; }

        [MaxLength(1000)]
        public string? PerformanceIndicators { get; set; }

        [Required]
        public AssessmentMethod AssessmentMethod { get; set; }

        [MaxLength(20)]
        public string? RatingScale { get; set; }

        public bool IsObservationBased { get; set; } = true;

        [MaxLength(500)]
        public string? ToolsRequired { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [MaxLength(1000)]
        public string? SpecificLearningOutcome { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateCompetencyAssessmentRequest
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

        [Required, MaxLength(100)]
        public string CompetencyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        [Required]
        public CBCLevel TargetLevel { get; set; }

        [MaxLength(1000)]
        public string? PerformanceIndicators { get; set; }

        [Required]
        public AssessmentMethod AssessmentMethod { get; set; }

        [MaxLength(20)]
        public string? RatingScale { get; set; }

        public bool IsObservationBased { get; set; }

        [MaxLength(500)]
        public string? ToolsRequired { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [MaxLength(1000)]
        public string? SpecificLearningOutcome { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Response DTO
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentScoreDto
    {
        public Guid Id { get; set; }
        public Guid? CompetencyAssessmentId { get; set; }
        public string? AssessmentTitle { get; set; }
        public string? CompetencyName { get; set; }

        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }

        public Guid? AssessorId { get; set; }
        public string? AssessorName { get; set; }

        public string Rating { get; set; } = string.Empty;
        public string CompetencyLevel { get; set; } = string.Empty;   // computed from Rating
        public int? ScoreValue { get; set; }
        public string? Evidence { get; set; }

        public DateTime AssessmentDate { get; set; }
        public string? AssessmentMethod { get; set; }
        public string? ToolsUsed { get; set; }
        public string? Feedback { get; set; }
        public string? AreasForImprovement { get; set; }
        public bool IsFinalized { get; set; }

        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
        public string? SpecificLearningOutcome { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Create Request
    // ═══════════════════════════════════════════════════════════════════

    public class CreateCompetencyAssessmentScoreRequest
    {
        [Required]
        public Guid CompetencyAssessmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        /// <summary>Exceeds | Meets | Approaching | Below</summary>
        [Required, MaxLength(50)]
        public string Rating { get; set; } = string.Empty;

        public int? ScoreValue { get; set; }

        [MaxLength(1000)]
        public string? Evidence { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        [MaxLength(20)]
        public string? AssessmentMethod { get; set; }

        [MaxLength(500)]
        public string? ToolsUsed { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(100)]
        public string? SpecificLearningOutcome { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Update Request
    // ═══════════════════════════════════════════════════════════════════

    public class UpdateCompetencyAssessmentScoreRequest
    {
        [Required, MaxLength(50)]
        public string Rating { get; set; } = string.Empty;

        public int? ScoreValue { get; set; }

        [MaxLength(1000)]
        public string? Evidence { get; set; }

        [Required]
        public DateTime AssessmentDate { get; set; }

        [MaxLength(20)]
        public string? AssessmentMethod { get; set; }

        [MaxLength(500)]
        public string? ToolsUsed { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(100)]
        public string? SpecificLearningOutcome { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FINALIZE — Bulk finalize scores for a competency assessment
    // ═══════════════════════════════════════════════════════════════════

    public class FinalizeCompetencyScoreRequest
    {
        [Required]
        public Guid ScoreId { get; set; }

        /// <summary>True to finalize, false to un-finalize.</summary>
        public bool IsFinalized { get; set; } = true;
    }

    public class BulkFinalizeCompetencyScoresRequest
    {
        [Required, MinLength(1)]
        public List<FinalizeCompetencyScoreRequest> Scores { get; set; } = new();
    }
}