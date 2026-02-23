using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class CompetencyAssessmentScore : TenantBaseEntity<Guid>
    {
        public Guid CompetencyAssessmentId { get; set; }
        public CompetencyAssessment CompetencyAssessment { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid? AssessorId { get; set; }
        public Teacher? Assessor { get; set; }

        [Required, MaxLength(50)]
        public string Rating { get; set; } = null!;

        public int? ScoreValue { get; set; }

        [MaxLength(1000)]
        public string? Evidence { get; set; }

        public DateTime AssessmentDate { get; set; }

        [MaxLength(20)]
        public string? AssessmentMethod { get; set; }

        [MaxLength(500)]
        public string? ToolsUsed { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        public bool IsFinalized { get; set; } = false;

        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(100)]
        public string? SpecificLearningOutcome { get; set; }

        // Computed — not persisted (Ignore in Fluent API)
        public string CompetencyLevel => Rating switch
        {
            "Exceeds" => "Excellent",
            "Meets" => "Proficient",
            "Approaching" => "Developing",
            "Below" => "Beginning",
            _ => "Not Assessed"
        };
    }
}