using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Assessments
{
    public class Assessment1 : TenantBaseEntity<Guid>
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)] // or 1000 depending on your design
        public string? Description { get; set; }

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid TermId { get; set; }
        public Term Term { get; set; } = null!;

        public Guid AcademicYearId { get; set; }
        public AcademicYear AcademicYear { get; set; } = null!;

        public DateTime AssessmentDate { get; set; }
        public decimal MaximumScore { get; set; }

        [Required, MaxLength(20)]
        public string AssessmentType { get; set; } = null!; // Formative, Summative, Competency

        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedDate { get; set; }

        public ICollection<Grade> Grades { get; set; } = new HashSet<Grade>();
    }


}