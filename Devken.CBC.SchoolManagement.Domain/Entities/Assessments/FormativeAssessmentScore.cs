using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class FormativeAssessmentScore : TenantBaseEntity<Guid>
    {
<<<<<<< HEAD
        public Guid FormativeAssessmentId { get; set; }     // Non-nullable — score must belong to an assessment
        public Guid StudentId { get; set; }
        public Guid? GradedById { get; set; }

        public decimal Score { get; set; }
        public decimal MaximumScore { get; set; }
=======
        public Guid FormativeAssessmentId { get; set; }
        public FormativeAssessment FormativeAssessment { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid? GradedById { get; set; }
        public Teacher? GradedBy { get; set; }

        public decimal Score { get; set; }
        public decimal MaximumScore { get; set; }

        // Not mapped — configured with Ignore() in Fluent API
>>>>>>> upstream/main
        public decimal Percentage => MaximumScore > 0 ? (Score / MaximumScore) * 100 : 0;

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
<<<<<<< HEAD
        public string? PerformanceLevel { get; set; }       // Excellent | Good | Satisfactory | Needs Improvement
=======
        public string? PerformanceLevel { get; set; }
>>>>>>> upstream/main

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        [MaxLength(500)]
        public string? Strengths { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        public bool IsSubmitted { get; set; } = false;
        public DateTime? SubmissionDate { get; set; }
        public DateTime? GradedDate { get; set; }

<<<<<<< HEAD
        // CBC tracking
=======
>>>>>>> upstream/main
        [MaxLength(100)]
        public string? CompetencyArea { get; set; }
        public bool CompetencyAchieved { get; set; } = false;
<<<<<<< HEAD

        // Navigation
        public FormativeAssessment FormativeAssessment { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public Teacher? GradedBy { get; set; }
=======
>>>>>>> upstream/main
    }
}