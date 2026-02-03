using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessment
{
    public class CompetencyAssessmentScore : TenantBaseEntity<Guid>
    {
        public Guid CompetencyAssessmentId { get; set; }

        public Guid StudentId { get; set; }

        public Guid? AssessorId { get; set; } // Teacher/Assessor who evaluated

        [Required]
        [MaxLength(50)]
        public string Rating { get; set; } = null!; // Exceeds, Meets, Approaching, Below

        public int? ScoreValue { get; set; } // Numeric score if applicable

        [MaxLength(1000)]
        public string? Evidence { get; set; } // Evidence of competency

        public DateTime AssessmentDate { get; set; }

        [MaxLength(20)]
        public string? AssessmentMethod { get; set; } // Observation, Oral, Written, Practical

        [MaxLength(500)]
        public string? ToolsUsed { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        public bool IsFinalized { get; set; } = false;

        // CBC Specific
        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(100)]
        public string? SpecificLearningOutcome { get; set; }

        // Navigation Properties
        public CompetencyAssessment CompetencyAssessment { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public Teacher? Assessor { get; set; }

        // Computed Properties
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