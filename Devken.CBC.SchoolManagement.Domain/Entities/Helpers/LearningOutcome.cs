using Devken.CBC.SchoolManagement.Domain.Common;

using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class LearningOutcome : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(200)]
        public string Outcome { get; set; } = null!;

        public CBCLevel Level { get; set; }

        [MaxLength(100)]
        public string? Strand { get; set; }

        [MaxLength(100)]
        public string? SubStrand { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; } // e.g., "MA1.1.1"

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsCore { get; set; } = true;

        // Navigation Properties
        public ICollection<FormativeAssessment> FormativeAssessments { get; set; } = new List<FormativeAssessment>();
    }
}
