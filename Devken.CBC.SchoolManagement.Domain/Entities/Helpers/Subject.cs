using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
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
    public class Subject : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        public CBCLevel Level { get; set; }

        [MaxLength(20)]
        public string? SubjectType { get; set; } // Core, Optional, Elective

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
