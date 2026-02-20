using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class LearningOutcome : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(250)]
        public string Outcome { get; set; } = null!;

        public CBCLevel Level { get; set; }

        // 🔗 Relationships

        [Required]
        public Guid LearningAreaId { get; set; }
        public LearningArea LearningArea { get; set; } = null!;

        [Required]
        public Guid StrandId { get; set; }
        public Strand Strand { get; set; } = null!;

        [Required]
        public Guid SubStrandId { get; set; }
        public SubStrand SubStrand { get; set; } = null!;

        // CBC Code (e.g., MA1.1.1)
        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsCore { get; set; } = true;

        // 🔗 Assessments mapped to this outcome
        public ICollection<FormativeAssessment> FormativeAssessments { get; set; }
            = new List<FormativeAssessment>();
    }
}