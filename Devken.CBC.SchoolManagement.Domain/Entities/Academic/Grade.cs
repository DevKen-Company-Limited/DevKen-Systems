using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class Grade : TenantBaseEntity<Guid>
    {
        public Guid StudentId { get; set; }

        public Guid SubjectId { get; set; }

        public Guid? TermId { get; set; }

        public Guid? AssessmentId { get; set; }

        [MaxLength(10)]
        public string? GradeLetter { get; set; } // A, B, C, D, E, F

        public decimal? Score { get; set; } // Percentage or points

        public decimal? MaximumScore { get; set; }

        [MaxLength(20)]
        public string? GradeType { get; set; } // Formative, Summative, Competency

        public DateTime AssessmentDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsFinalized { get; set; } = false;

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public Term? Term { get; set; }
        public Assessment? Assessment { get; set; }
    }
}