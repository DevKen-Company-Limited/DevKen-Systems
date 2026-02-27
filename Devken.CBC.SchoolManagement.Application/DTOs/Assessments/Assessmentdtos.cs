// Devken.CBC.SchoolManagement.Application/DTOs/Assessments/AssessmentDTOs.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.ComponentModel.DataAnnotations;

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
    // LIST ITEM (lightweight — for table/grid views)
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public AssessmentTypeDto AssessmentType { get; set; }
        public string TeacherName { get; set; } = "-";
        public string SubjectName { get; set; } = "-";
        public string ClassName { get; set; } = "-";
        public string TermName { get; set; } = "-";
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; }
        public int ScoreCount { get; set; }

        // Formative-only (null for other types)
        public string? StrandName { get; set; }
        public string? SubStrandName { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FULL RESPONSE (detail view — includes all type-specific fields)
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentResponse
    {
        public Guid Id { get; set; }
        public AssessmentTypeDto AssessmentType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // ── FIX: School that owns this assessment (= TenantId in DB) ──────────
        // Previously missing from the API response, causing the school dropdown
        // to be blank for SuperAdmin on edit. Now returned as schoolId in JSON.
        public Guid SchoolId { get; set; }

        // ── People & Class ────────────────────────────────────────────────────
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

        // ── Core fields ───────────────────────────────────────────────────────
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ScoreCount { get; set; }

        // ── Formative-specific ────────────────────────────────────────────────
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
        public bool? RequiresRubric { get; set; }
        public decimal? AssessmentWeight { get; set; }
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ────────────────────────────────────────────────
        public string? ExamType { get; set; }
        public int? Duration { get; set; }
        public int? NumberOfQuestions { get; set; }
        public decimal? PassMark { get; set; }
        public bool? HasPracticalComponent { get; set; }
        public decimal? PracticalWeight { get; set; }
        public decimal? TheoryWeight { get; set; }
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ───────────────────────────────────────────────
        public string? CompetencyName { get; set; }
        public string? CompetencyStrand { get; set; }
        public string? CompetencySubStrand { get; set; }
        public Domain.Enums.CBCLevel? TargetLevel { get; set; }
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod? AssessmentMethod { get; set; }
        public string? RatingScale { get; set; }
        public bool? IsObservationBased { get; set; }
        public string? ToolsRequired { get; set; }
        public string? CompetencyInstructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE RESPONSE
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentScoreResponse
    {
        public Guid Id { get; set; }
        public AssessmentTypeDto AssessmentType { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; } = "-";
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = "-";
        public string StudentAdmissionNo { get; set; } = "-";
        public DateTime AssessmentDate { get; set; }

        // Formative score fields
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

        // Summative score fields
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? MaximumTotalScore { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public bool? IsPassed { get; set; }
        public string? PerformanceStatus { get; set; }
        public string? Comments { get; set; }

        // Competency score fields
        public string? Rating { get; set; }
        public string? CompetencyLevel { get; set; }
        public string? Evidence { get; set; }
        public bool? IsFinalized { get; set; }
        public string? AssessorName { get; set; }
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateAssessmentRequest
    {
        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }

        // Tenant resolution
        public Guid? TenantId { get; set; }
        public Guid? SchoolId { get; set; }

        // ── Shared ────────────────────────────────────────────────────────────
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

        // ── Formative-specific ────────────────────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? StrandId { get; set; }
        public Guid? SubStrandId { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? Criteria { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; }
        public decimal AssessmentWeight { get; set; } = 100.0m;
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ────────────────────────────────────────────────
        public string? ExamType { get; set; }
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; } = 50.0m;
        public bool HasPracticalComponent { get; set; }
        public decimal PracticalWeight { get; set; }
        public decimal TheoryWeight { get; set; } = 100.0m;
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ───────────────────────────────────────────────
        public string? CompetencyName { get; set; }
        public string? CompetencyStrand { get; set; }
        public string? CompetencySubStrand { get; set; }
        public object? TargetLevel { get; set; }
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod AssessmentMethod { get; set; }
        public string? RatingScale { get; set; }
        public bool IsObservationBased { get; set; } = true;
        public string? ToolsRequired { get; set; }
        public string? CompetencyInstructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class UpdateAssessmentRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }

        // ── Shared ────────────────────────────────────────────────────────────
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

        // ── Formative-specific ────────────────────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? StrandId { get; set; }
        public Guid? SubStrandId { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? Criteria { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; }
        public decimal AssessmentWeight { get; set; } = 100.0m;
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ────────────────────────────────────────────────
        public string? ExamType { get; set; }
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; } = 50.0m;
        public bool HasPracticalComponent { get; set; }
        public decimal PracticalWeight { get; set; }
        public decimal TheoryWeight { get; set; } = 100.0m;
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ───────────────────────────────────────────────
        public string? CompetencyName { get; set; }
        public string? CompetencyStrand { get; set; }
        public string? CompetencySubStrand { get; set; }
        public object? TargetLevel { get; set; }
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod AssessmentMethod { get; set; }
        public string? RatingScale { get; set; }
        public bool IsObservationBased { get; set; } = true;
        public string? ToolsRequired { get; set; }
        public string? CompetencyInstructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }
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
    // UPSERT SCORE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class UpsertScoreRequest
    {
        [Required]
        public Guid AssessmentId { get; set; }

        [Required]
        public AssessmentTypeDto AssessmentType { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        // ── Formative score fields ─────────────────────────────────────────────
        public decimal? Score { get; set; }
        public decimal? MaximumScore { get; set; }
        public string? Grade { get; set; }
        public string? PerformanceLevel { get; set; }
        public string? Feedback { get; set; }
        public string? Strengths { get; set; }
        public string? AreasForImprovement { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string? CompetencyArea { get; set; }
        public bool CompetencyAchieved { get; set; }
        public Guid? GradedById { get; set; }

        // ── Summative score fields ─────────────────────────────────────────────
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? MaximumTheoryScore { get; set; }
        public decimal? MaximumPracticalScore { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }
        public bool IsPassed { get; set; }
        public string? Comments { get; set; }

        // ── Competency score fields ────────────────────────────────────────────
        public string? Rating { get; set; }
        public int? ScoreValue { get; set; }
        public string? Evidence { get; set; }
        public string? AssessmentMethod { get; set; }
        public string? ToolsUsed { get; set; }
        public bool IsFinalized { get; set; }
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
        public string? SpecificLearningOutcome { get; set; }
        public Guid? AssessorId { get; set; }
    }
}