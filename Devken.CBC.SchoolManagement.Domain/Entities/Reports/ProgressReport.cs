using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Reports
{
    public class ProgressReport : TenantBaseEntity<Guid>
    {
        public Guid StudentId { get; set; }

        public Guid ClassId { get; set; }

        public Guid TermId { get; set; }

        public Guid AcademicYearId { get; set; }

        [MaxLength(20)]
        public string ReportType { get; set; } = null!; // Termly, MidTerm, EndTerm

        public DateTime ReportDate { get; set; }

        public decimal? OverallScore { get; set; }

        [MaxLength(10)]
        public string? OverallGrade { get; set; }

        public int? ClassPosition { get; set; }

        public int? StreamPosition { get; set; }

        [MaxLength(2000)]
        public string? ClassTeacherRemarks { get; set; }

        [MaxLength(2000)]
        public string? HeadTeacherRemarks { get; set; }

        public DateTime? NextReportDate { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedDate { get; set; }

        // CBC Specific Fields
        [MaxLength(2000)]
        public string? CompetencyRemarks { get; set; }

        [MaxLength(2000)]
        public string? CoCurricularRemarks { get; set; }

        [MaxLength(2000)]
        public string? BehaviorRemarks { get; set; }

        public bool RequiresParentConference { get; set; } = false;

        // Navigation Properties
        public Student Student { get; set; } = null!;
        public Class Class { get; set; } = null!;
        public Term Term { get; set; } = null!;
        public AcademicYear AcademicYear { get; set; } = null!;
        public ICollection<SubjectReport> SubjectReports { get; set; } = new List<SubjectReport>();
        public ICollection<ProgressReportComment> Comments { get; set; } = new List<ProgressReportComment>();
    }
}