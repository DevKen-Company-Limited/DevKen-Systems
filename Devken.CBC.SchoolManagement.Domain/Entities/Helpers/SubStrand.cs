using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class SubStrand : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;   // e.g. Whole Numbers

        // 🔗 Relationships
        [Required]
        public Guid StrandId { get; set; }
        public Strand Strand { get; set; } = null!;

        public ICollection<LearningOutcome> LearningOutcomes { get; set; } = new List<LearningOutcome>();
    }
}