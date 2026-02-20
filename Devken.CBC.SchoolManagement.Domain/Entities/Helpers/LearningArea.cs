using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class LearningArea : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;   // e.g. Mathematics

        [MaxLength(20)]
        public string? Code { get; set; }           // e.g. "MA"

        public CBCLevel Level { get; set; }

        public ICollection<Strand> Strands { get; set; }
            = new List<Strand>();
    }
}