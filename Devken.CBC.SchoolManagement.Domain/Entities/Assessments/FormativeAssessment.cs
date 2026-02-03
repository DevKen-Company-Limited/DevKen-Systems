using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class FormativeAssessment : Assessment1
    {
        [MaxLength(50)]
        public string? FormativeType { get; set; } // Quiz, Assignment, Project, Presentation

        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public int? LearningOutcomeId { get; set; }

        [MaxLength(1000)]
        public string? FeedbackTemplate { get; set; }

        public bool RequiresRubric { get; set; } = false;

        // For CBC specific tracking
        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        // Navigation Properties
        public ICollection<FormativeAssessmentScore> Scores { get; set; } = new List<FormativeAssessmentScore>();
    }
}