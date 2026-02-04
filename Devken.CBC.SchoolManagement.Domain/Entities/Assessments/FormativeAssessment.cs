using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class FormativeAssessment : Assessment1
    {
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

        // ✅ Newly added to match configuration
        [MaxLength(500)]
        public string? Criteria { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        public decimal AssessmentWeight { get; set; } = 100.0m;

        // Navigation Properties
        public LearningOutcome? LearningOutcome { get; set; }
        public ICollection<FormativeAssessmentScore> Scores { get; set; } = new List<FormativeAssessmentScore>();
    }
}
