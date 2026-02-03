using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class Assessment : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public Guid SubjectId { get; set; }

        public Guid ClassId { get; set; }

        public Guid TermId { get; set; }

        public Guid AcademicYearId { get; set; }

        public DateTime AssessmentDate { get; set; }

        public decimal MaximumScore { get; set; }

        [MaxLength(20)]
        public string AssessmentType { get; set; } = null!; // Formative, Summative, Competency

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedDate { get; set; }

        // Navigation Properties
        public Subject Subject { get; set; } = null!;
        public Class Class { get; set; } = null!;
        public Term Term { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}