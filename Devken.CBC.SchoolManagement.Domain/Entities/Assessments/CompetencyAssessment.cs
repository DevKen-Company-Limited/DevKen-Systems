// Devken.CBC.SchoolManagement.Domain/Entities/Assessments/CompetencyAssessment.cs
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class CompetencyAssessment : Assessment1
    {
        [Required, MaxLength(100)]
        public string CompetencyName { get; set; } = null!;

        // Stored as strings for Competency — no FK constraint; can be ad-hoc labels
        [MaxLength(100)]
        public string? CompetencyStrand { get; set; }

        [MaxLength(100)]
        public string? CompetencySubStrand { get; set; }

        public CBCLevel TargetLevel { get; set; }

        [MaxLength(1000)]
        public string? PerformanceIndicators { get; set; }

        public AssessmentMethod AssessmentMethod { get; set; }

        /// <summary>Exceeds | Meets | Approaching | Below</summary>
        [MaxLength(20)]
        public string? RatingScale { get; set; }

        public bool IsObservationBased { get; set; } = true;

        [MaxLength(500)]
        public string? ToolsRequired { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [MaxLength(1000)]
        public string? SpecificLearningOutcome { get; set; }

        public ICollection<CompetencyAssessmentScore> Scores { get; set; }
            = new List<CompetencyAssessmentScore>();
    }

    public enum AssessmentMethod
    {
        Observation = 1,
        OralQuestioning = 2,
        WrittenTask = 3,
        PracticalTask = 4,
        Portfolio = 5,
        Project = 6,
        Other = 7
    }
}