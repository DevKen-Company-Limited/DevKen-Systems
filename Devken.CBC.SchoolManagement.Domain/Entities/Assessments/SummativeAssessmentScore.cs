using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class SummativeAssessmentScore : TenantBaseEntity<Guid>
    {
        public Guid? SummativeAssessmentId { get; set; }

        public Guid StudentId { get; set; }

        public decimal TheoryScore { get; set; }

        public decimal? PracticalScore { get; set; }

        public decimal MaximumTheoryScore { get; set; }

        public decimal? MaximumPracticalScore { get; set; }

        public decimal TotalScore => TheoryScore + (PracticalScore ?? 0);

        public decimal MaximumTotalScore => MaximumTheoryScore + (MaximumPracticalScore ?? 0);

        public decimal Percentage => MaximumTotalScore > 0 ? (TotalScore / MaximumTotalScore) * 100 : 0;

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? Remarks { get; set; } // Distinction, Credit, Pass, Fail

        public int? PositionInClass { get; set; }

        public int? PositionInStream { get; set; }

        public bool IsPassed { get; set; }

        public DateTime? GradedDate { get; set; }

        public Guid? GradedById { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        // Navigation Properties
        public SummativeAssessment? SummativeAssessment { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public Teacher? GradedBy { get; set; }

        // Computed Properties
        public string PerformanceStatus
        {
            get
            {
                if (Percentage >= 80) return "Excellent";
                if (Percentage >= 70) return "Very Good";
                if (Percentage >= 60) return "Good";
                if (Percentage >= 50) return "Average";
                if (Percentage >= 40) return "Below Average";
                return "Poor";
            }
        }
    }
}