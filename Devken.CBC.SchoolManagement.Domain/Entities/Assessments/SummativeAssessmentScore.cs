using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class SummativeAssessmentScore : TenantBaseEntity<Guid>
    {
        public Guid SummativeAssessmentId { get; set; }
        public SummativeAssessment SummativeAssessment { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid? GradedById { get; set; }
        public Teacher? GradedBy { get; set; }

        public decimal TheoryScore { get; set; }
        public decimal? PracticalScore { get; set; }
        public decimal MaximumTheoryScore { get; set; }
        public decimal? MaximumPracticalScore { get; set; }

        // Computed — not persisted (Ignore in Fluent API)
        public decimal TotalScore => TheoryScore + (PracticalScore ?? 0);
        public decimal MaximumTotalScore => MaximumTheoryScore + (MaximumPracticalScore ?? 0);
        public decimal Percentage => MaximumTotalScore > 0 ? (TotalScore / MaximumTotalScore) * 100 : 0;
        public string PerformanceStatus => Percentage switch
        {
            >= 80 => "Excellent",
            >= 70 => "Very Good",
            >= 60 => "Good",
            >= 50 => "Average",
            >= 40 => "Below Average",
            _ => "Poor"
        };

        [MaxLength(10)]
        public string? Grade { get; set; }

        [MaxLength(20)]
        public string? Remarks { get; set; }

        public int? PositionInClass { get; set; }
        public int? PositionInStream { get; set; }
        public bool IsPassed { get; set; }
        public DateTime? GradedDate { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }
}