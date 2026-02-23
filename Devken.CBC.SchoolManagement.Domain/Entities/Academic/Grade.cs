using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic
{
    public class Grade : TenantBaseEntity<Guid>
    {
        public Guid StudentId { get; set; }

        public Guid SubjectId { get; set; }

        public Guid? TermId { get; set; }

        public Guid? AssessmentId { get; set; }

        public GradeLetter? GradeLetter { get; set; }

        public decimal? Score { get; set; }

        public decimal? MaximumScore { get; set; }

        public AssessmentType? GradeType { get; set; }

        public DateTime AssessmentDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsFinalized { get; set; } = false;

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public Term? Term { get; set; }
        public Assessment1? Assessment { get; set; }
    }
}