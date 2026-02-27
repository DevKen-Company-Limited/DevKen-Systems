using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{



    public class Term : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        public int TermNumber { get; set; } // 1, 2, 3

        public Guid AcademicYearId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        public AcademicYear AcademicYear { get; set; } = null!;

        public ICollection<Assessment1> Assessments { get; set; }
            = new List<Assessment1>();

        public ICollection<ProgressReport> ProgressReports { get; set; }
            = new List<ProgressReport>();

        // ✅ ADD THIS
        public ICollection<Grade> Grades { get; set; }
            = new List<Grade>();
    }
}
