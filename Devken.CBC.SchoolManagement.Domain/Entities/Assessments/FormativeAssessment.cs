// Devken.CBC.SchoolManagement.Domain/Entities/Assessments/FormativeAssessment.cs
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class FormativeAssessment : Assessment1
    {
        // ── Formative-specific metadata ───────────────────────────────────────
        [MaxLength(50)]
        public string? FormativeType { get; set; }          // Quiz | Homework | Observation | etc.

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        [MaxLength(500)]
        public string? Criteria { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [MaxLength(1000)]
        public string? FeedbackTemplate { get; set; }

        public bool RequiresRubric { get; set; } = false;

        [Range(0, 100)]
        public decimal AssessmentWeight { get; set; } = 100.0m;

        // ── CBC Curriculum Hierarchy FKs ─────────────────────────────────────
        // Links to the CBC strand/sub-strand/learning outcome hierarchy
        // so that assessments are mapped to specific curriculum nodes.

        public Guid? StrandId { get; set; }
        public Strand? Strand { get; set; }

        public Guid? SubStrandId { get; set; }
        public SubStrand? SubStrand { get; set; }

        public Guid? LearningOutcomeId { get; set; }
        public LearningOutcome? LearningOutcome { get; set; }

        // ── Scores ────────────────────────────────────────────────────────────
        public ICollection<FormativeAssessmentScore> Scores { get; set; }
            = new List<FormativeAssessmentScore>();
    }
}