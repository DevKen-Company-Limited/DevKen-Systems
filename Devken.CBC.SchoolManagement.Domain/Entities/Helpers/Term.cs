using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
        public ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();
    }
}
