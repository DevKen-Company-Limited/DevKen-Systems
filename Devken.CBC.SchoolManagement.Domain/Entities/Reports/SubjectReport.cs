using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Reports
{
    public class SubjectReport : TenantBaseEntity<Guid>
    {
        public Guid ProgressReportId { get; set; }

        public Guid SubjectId { get; set; }

        public Guid? TeacherId { get; set; }

        // Assessment Scores
        public decimal? FormativeScore { get; set; }

        public decimal? SummativeScore { get; set; }

        public decimal? CompetencyScore { get; set; }

        public decimal? TotalScore { get; set; }

        public decimal? MaximumScore { get; set; }

        public decimal? Percentage => MaximumScore > 0 ? (TotalScore / MaximumScore) * 100 : null;

        [MaxLength(10)]
        public string? Grade { get; set; }

        public int? SubjectPosition { get; set; }

        // Teacher Comments
        [MaxLength(1000)]
        public string? TeacherRemarks { get; set; }

        [MaxLength(500)]
        public string? Strengths { get; set; }

        [MaxLength(500)]
        public string? AreasForImprovement { get; set; }

        [MaxLength(500)]
        public string? Recommendations { get; set; }

        // CBC Specific
        [MaxLength(1000)]
        public string? CompetencyFeedback { get; set; }

        public bool CompetencyAchieved { get; set; } = false;

        // Navigation Properties
        public ProgressReport ProgressReport { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public Teacher? SubjectTeacher { get; set; }

        // Computed Properties
        public string PerformanceLevel
        {
            get
            {
                if (!Percentage.HasValue) return "Not Assessed";
                return Percentage.Value switch
                {
                    >= 80 => "Excellent",
                    >= 70 => "Very Good",
                    >= 60 => "Good",
                    >= 50 => "Average",
                    >= 40 => "Below Average",
                    _ => "Needs Improvement"
                };
            }
        }
    }
}