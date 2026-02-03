using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class CompetencyAssessment : Assessment1
    {
        [MaxLength(100)]
        public string CompetencyName { get; set; } = null!;

        [MaxLength(50)]
        public string? Strand { get; set; }

        [MaxLength(50)]
        public string? SubStrand { get; set; }

        public CBCLevel TargetLevel { get; set; }

        [MaxLength(1000)]
        public string? PerformanceIndicators { get; set; }

        public AssessmentMethod AssessmentMethod { get; set; }

        [MaxLength(20)]
        public string? RatingScale { get; set; } // Exceeds, Meets, Approaching, Below

        public bool IsObservationBased { get; set; } = true;

        [MaxLength(500)]
        public string? ToolsRequired { get; set; }

        // Navigation Properties
        public ICollection<CompetencyAssessmentScore> Scores { get; set; } = new List<CompetencyAssessmentScore>();
    }
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