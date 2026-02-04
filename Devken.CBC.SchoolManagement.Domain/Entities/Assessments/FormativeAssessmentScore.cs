using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class FormativeAssessmentScore : TenantBaseEntity<Guid>
    {
        public Guid? FormativeAssessmentId { get; set; }

        public Guid StudentId { get; set; }

        public decimal Score { get; set; }

        public decimal MaximumScore { get; set; }

        public decimal Percentage => MaximumScore > 0 ? (Score / MaximumScore) * 100 : 0;

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? PerformanceLevel { get; set; } // Excellent, Good, Satisfactory, Needs Improvement

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? Strengths { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        public bool IsSubmitted { get; set; } = false;

        public DateTime? SubmissionDate { get; set; }

        public DateTime? GradedDate { get; set; }

        public Guid? GradedById { get; set; }

        // For CBC tracking
        [MaxLength(100)]
        public string? CompetencyArea { get; set; }

        public bool CompetencyAchieved { get; set; } = false;

        // Navigation Properties
        public FormativeAssessment? FormativeAssessment { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public Teacher? GradedBy { get; set; }
    }
}