using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Curriculum
{
    // ── Response ─────────────────────────────────────────────────────────────
    public class LearningAreaResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Level { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateLearningAreaDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Code { get; set; }

        [Required]
        public CBCLevel Level { get; set; }

        /// <summary>Required when caller is SuperAdmin; ignored for school users.</summary>
        public Guid? TenantId { get; set; }
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public class UpdateLearningAreaDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Code { get; set; }

        [Required]
        public CBCLevel Level { get; set; }
    }
}
