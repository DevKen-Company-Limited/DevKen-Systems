using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class Strand : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;   // e.g. Numbers

        [Required]
        public Guid LearningAreaId { get; set; }
        public LearningArea LearningArea { get; set; } = null!;

        public ICollection<SubStrand> SubStrands { get; set; }
            = new List<SubStrand>();
    }
}