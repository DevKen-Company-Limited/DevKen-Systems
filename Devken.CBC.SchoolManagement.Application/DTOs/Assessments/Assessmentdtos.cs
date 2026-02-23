// Devken.CBC.SchoolManagement.Application/DTOs/Assessments/AssessmentDtos.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ─────────────────────────────────────────────────────────────────────────
    // ENUM
    // ─────────────────────────────────────────────────────────────────────────
    public enum AssessmentTypeDto
    {
        Formative = 1,
        Summative = 2,
        Competency = 3
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateAssessmentRequest
    {
        // ── Shared ───────────────────────────────────────────────────────────
        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }

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

        public bool IsPublished { get; set; } = false;

        /// <summary>Set by controller from JWT; SuperAdmin may override.</summary>
        public Guid? TenantId { get; set; }

        /// <summary>Frontend alias for TenantId. Controller resolves TenantId = TenantId ?? SchoolId.</summary>
        [JsonPropertyName("schoolId")]
        public Guid? SchoolId { get; set; }

        // ── Formative-specific ────────────────────────────────────────────────
        [MaxLength(50)]
        public string? FormativeType { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        // CBC curriculum hierarchy — IDs for structured lookup
        public Guid? StrandId { get; set; }
        public Guid? SubStrandId { get; set; }
        public Guid? LearningOutcomeId { get; set; }

        [MaxLength(500)]
        public string? Criteria { get; set; }

        [MaxLength(1000)]
        public string? FeedbackTemplate { get; set; }

        public bool RequiresRubric { get; set; } = false;

        [Range(0, 100)]
        public decimal AssessmentWeight { get; set; } = 100.0m;

        [MaxLength(1000)]
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ────────────────────────────────────────────────
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
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ───────────────────────────────────────────────
        [MaxLength(100)]
        public string? CompetencyName { get; set; }

        [MaxLength(100)]
        public string? CompetencyStrand { get; set; }

        [MaxLength(100)]
        public string? CompetencySubStrand { get; set; }

        public CBCLevel TargetLevel { get; set; }

        [MaxLength(1000)]
        public string? PerformanceIndicators { get; set; }

        public AssessmentMethod AssessmentMethod { get; set; }

        [MaxLength(20)]
        public string? RatingScale { get; set; }

        public bool IsObservationBased { get; set; } = true;

        [MaxLength(500)]
        public string? ToolsRequired { get; set; }

        [MaxLength(1000)]
        public string? CompetencyInstructions { get; set; }

        [MaxLength(1000)]
        public string? SpecificLearningOutcome { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class UpdateAssessmentRequest : CreateAssessmentRequest
    {
        [Required]
        public Guid Id { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLISH REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class PublishAssessmentRequest
    {
        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIST ITEM — lightweight for grid/table views
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public AssessmentTypeDto AssessmentType { get; set; }
        public string AssessmentTypeLabel => AssessmentType.ToString();
        public string TeacherName { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string TermName { get; set; } = null!;
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; }
        public int ScoreCount { get; set; }

        // Formative extras shown in grid
        public string? StrandName { get; set; }
        public string? SubStrandName { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FULL RESPONSE — all fields; UI reads AssessmentType to show relevant ones
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentResponse
    {
        // ── Shared ───────────────────────────────────────────────────────────
        public Guid Id { get; set; }
        public AssessmentTypeDto AssessmentType { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = null!;
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = null!;
        public Guid AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = null!;
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ScoreCount { get; set; }

        // ── Formative ─────────────────────────────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? StrandId { get; set; }
        public string? StrandName { get; set; }
        public Guid? SubStrandId { get; set; }
        public string? SubStrandName { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? LearningOutcomeName { get; set; }
        public string? Criteria { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; }
        public decimal AssessmentWeight { get; set; }
        public string? FormativeInstructions { get; set; }

        // ── Summative ─────────────────────────────────────────────────────────
        public string? ExamType { get; set; }
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; }
        public bool HasPracticalComponent { get; set; }
        public decimal PracticalWeight { get; set; }
        public decimal TheoryWeight { get; set; }
        public string? SummativeInstructions { get; set; }

        // ── Competency ────────────────────────────────────────────────────────
        public string? CompetencyName { get; set; }
        public string? CompetencyStrand { get; set; }
        public string? CompetencySubStrand { get; set; }
        public CBCLevel? TargetLevel { get; set; }
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod? AssessmentMethod { get; set; }
        public string? RatingScale { get; set; }
        public bool IsObservationBased { get; set; }
        public string? ToolsRequired { get; set; }
        public string? CompetencyInstructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE DTOs
    // ─────────────────────────────────────────────────────────────────────────

    public class UpsertScoreRequest
    {
        public Guid? ScoreId { get; set; }                  // null = create, set = update

        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }

        [Required]
        public Guid AssessmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        // ── Formative score fields ────────────────────────────────────────────
        public decimal? Score { get; set; }
        public decimal? MaximumScore { get; set; }

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

        public bool IsSubmitted { get; set; } = false;
        public DateTime? SubmissionDate { get; set; }

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }
        public bool CompetencyAchieved { get; set; } = false;

        public Guid? GradedById { get; set; }

        // ── Summative score fields ────────────────────────────────────────────
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? MaximumTheoryScore { get; set; }
        public decimal? MaximumPracticalScore { get; set; }

        [MaxLength(20)]
        public string? Remarks { get; set; }

        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }
        public bool IsPassed { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        // ── Competency score fields ───────────────────────────────────────────
        [MaxLength(50)]
        public string? Rating { get; set; }

        public int? ScoreValue { get; set; }

        [MaxLength(1000)]
        public string? Evidence { get; set; }

        [MaxLength(20)]
        public string? AssessmentMethod { get; set; }

        [MaxLength(500)]
        public string? ToolsUsed { get; set; }

        public bool IsFinalized { get; set; } = false;

        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(500)]
        public string? SpecificLearningOutcome { get; set; }

        public Guid? AssessorId { get; set; }
    }

    public class AssessmentScoreResponse
    {
        public Guid Id { get; set; }
        public AssessmentTypeDto AssessmentType { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; } = null!;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string StudentAdmissionNo { get; set; } = null!;
        public DateTime AssessmentDate { get; set; }

        // Formative
        public decimal? Score { get; set; }
        public decimal? MaximumScore { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
        public string? PerformanceLevel { get; set; }
        public string? Feedback { get; set; }
        public string? Strengths { get; set; }
        public bool? CompetencyAchieved { get; set; }
        public bool? IsSubmitted { get; set; }
        public string? GradedByName { get; set; }

        // Summative
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? MaximumTotalScore { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public bool? IsPassed { get; set; }
        public string? PerformanceStatus { get; set; }
        public string? Comments { get; set; }

        // Competency
        public string? Rating { get; set; }
        public string? CompetencyLevel { get; set; }
        public string? Evidence { get; set; }
        public bool? IsFinalized { get; set; }
        public string? AssessorName { get; set; }
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
    }
}