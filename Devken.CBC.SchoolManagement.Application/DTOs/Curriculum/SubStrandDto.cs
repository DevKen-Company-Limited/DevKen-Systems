using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Curriculum
{
    public class SubStrandResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid StrandId { get; set; }
        public string? StrandName { get; set; }
        public Guid LearningAreaId { get; set; }
        public string? LearningAreaName { get; set; }
        public Guid TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateSubStrandDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        public Guid StrandId { get; set; }

        /// <summary>Required when caller is SuperAdmin; ignored for school users.</summary>
        public Guid? TenantId { get; set; }
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public class UpdateSubStrandDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        public Guid StrandId { get; set; }
    }
}
