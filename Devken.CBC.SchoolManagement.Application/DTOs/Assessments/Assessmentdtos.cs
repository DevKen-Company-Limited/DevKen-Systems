<<<<<<< HEAD
﻿using System;
using System.ComponentModel.DataAnnotations;
=======
﻿using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.Text.Json.Serialization;
>>>>>>> upstream/main

namespace Devken.CBC.SchoolManagement.Application.DTOs.Assessments
{
    // ─────────────────────────────────────────────────────────────────────────
<<<<<<< HEAD
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
=======
    // ENUMS
    // ─────────────────────────────────────────────────────────────────────────
    public enum AssessmentTypeDto
    {
        Formative = 1,
        Summative = 2,
        Competency = 3
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE / UPDATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class CreateAssessmentRequest
    {
        // ── Shared ──────────────────────────────────────────────────────
        public AssessmentTypeDto AssessmentType { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid ClassId { get; set; }
        public Guid TermId { get; set; }
        public Guid AcademicYearId { get; set; }
        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }
        public bool IsPublished { get; set; } = false;
        public Guid? TenantId { get; set; }             // Required for SuperAdmin
        /// <summary>
        /// Frontend alias for TenantId — the Angular form sends "schoolId".
        /// The controller resolves TenantId = TenantId ?? SchoolId before
        /// calling the service, so the service always uses TenantId.
        /// </summary>
        [JsonPropertyName("schoolId")]
        public Guid? SchoolId { get; set; }

        // ── Formative-specific ───────────────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? FormativeStrand { get; set; }
        public string? FormativeSubStrand { get; set; }
        public string? Criteria { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; } = false;
        public decimal AssessmentWeight { get; set; } = 100.0m;
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ───────────────────────────────────────────
        public string? ExamType { get; set; }
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; } = 50.0m;
        public bool HasPracticalComponent { get; set; } = false;
        public decimal PracticalWeight { get; set; } = 0.0m;
        public decimal TheoryWeight { get; set; } = 100.0m;
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ──────────────────────────────────────────
        public string? CompetencyName { get; set; }
        public string? CompetencyStrand { get; set; }
        public string? CompetencySubStrand { get; set; }
        public CBCLevel TargetLevel { get; set; }
        public string? PerformanceIndicators { get; set; }
        public AssessmentMethod AssessmentMethod { get; set; }
        public string? RatingScale { get; set; }
        public bool IsObservationBased { get; set; } = true;
        public string? ToolsRequired { get; set; }
        public string? CompetencyInstructions { get; set; }
        public string? SpecificLearningOutcome { get; set; }
    }

    public class UpdateAssessmentRequest : CreateAssessmentRequest
    {
        public Guid Id { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ASSESSMENT RESPONSE
    // ─────────────────────────────────────────────────────────────────────────
    public class AssessmentResponse
    {
        // ── Shared ──────────────────────────────────────────────────────
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

        // ── Formative-specific ───────────────────────────────────────────
        public string? FormativeType { get; set; }
        public string? CompetencyArea { get; set; }
        public Guid? LearningOutcomeId { get; set; }
        public string? LearningOutcomeName { get; set; }
        public string? FormativeStrand { get; set; }
        public string? FormativeSubStrand { get; set; }
        public string? Criteria { get; set; }
        public string? FeedbackTemplate { get; set; }
        public bool RequiresRubric { get; set; }
        public decimal AssessmentWeight { get; set; }
        public string? FormativeInstructions { get; set; }

        // ── Summative-specific ───────────────────────────────────────────
        public string? ExamType { get; set; }
        public TimeSpan? Duration { get; set; }
        public int NumberOfQuestions { get; set; }
        public decimal PassMark { get; set; }
        public bool HasPracticalComponent { get; set; }
        public decimal PracticalWeight { get; set; }
        public decimal TheoryWeight { get; set; }
        public string? SummativeInstructions { get; set; }

        // ── Competency-specific ──────────────────────────────────────────
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
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLISH REQUEST
    // ─────────────────────────────────────────────────────────────────────────
    public class PublishAssessmentRequest
    {
        public AssessmentTypeDto AssessmentType { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE DTOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Single request that covers score entry for all three assessment types.
    /// Only populate the fields relevant to the AssessmentType.
    /// </summary>
    public class UpsertScoreRequest
    {
        public Guid? ScoreId { get; set; }                // null = create, set = update
        public AssessmentTypeDto AssessmentType { get; set; }
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }

        // ── Formative score fields ───────────────────────────────────────
        public decimal? Score { get; set; }
        public decimal? MaximumScore { get; set; }
        public string? Grade { get; set; }
        public string? PerformanceLevel { get; set; }
        public string? Feedback { get; set; }
        public string? Strengths { get; set; }
        public string? AreasForImprovement { get; set; }
        public bool IsSubmitted { get; set; } = false;
        public DateTime? SubmissionDate { get; set; }
        public string? CompetencyArea { get; set; }
        public bool CompetencyAchieved { get; set; } = false;
        public Guid? GradedById { get; set; }

        // ── Summative score fields ───────────────────────────────────────
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? MaximumTheoryScore { get; set; }
        public decimal? MaximumPracticalScore { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }
        public bool IsPassed { get; set; }
        public string? Comments { get; set; }

        // ── Competency score fields ──────────────────────────────────────
        public string? Rating { get; set; }
        public int? ScoreValue { get; set; }
        public string? Evidence { get; set; }
        public string? AssessmentMethod { get; set; }
        public string? ToolsUsed { get; set; }
        public bool IsFinalized { get; set; } = false;
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
        public string? SpecificLearningOutcome { get; set; }
        public Guid? AssessorId { get; set; }
    }

    /// <summary>
    /// Unified score response — all fields present, non-relevant ones are null.
    /// The UI reads AssessmentType to know which fields to display.
    /// </summary>
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

        // ── Formative ────────────────────────────────────────────────────
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

        // ── Summative ────────────────────────────────────────────────────
        public decimal? TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal? MaximumTotalScore { get; set; }
        public string? Remarks { get; set; }
        public int? PositionInClass { get; set; }
        public bool? IsPassed { get; set; }
        public string? PerformanceStatus { get; set; }
        public string? Comments { get; set; }

        // ── Competency ───────────────────────────────────────────────────
        public string? Rating { get; set; }
        public string? CompetencyLevel { get; set; }
        public string? Evidence { get; set; }
        public bool? IsFinalized { get; set; }
        public string? AssessorName { get; set; }
        public string? Strand { get; set; }
        public string? SubStrand { get; set; }
>>>>>>> upstream/main
    }
}