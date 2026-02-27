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
    public class LearningOutcomeResponseDto
    {
        public Guid Id { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string Level { get; set; } = string.Empty;
        public bool IsCore { get; set; }

        // Hierarchy breadcrumbs
        public Guid LearningAreaId { get; set; }
        public string? LearningAreaName { get; set; }
        public Guid StrandId { get; set; }
        public string? StrandName { get; set; }
        public Guid SubStrandId { get; set; }
        public string? SubStrandName { get; set; }

        public Guid TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateLearningOutcomeDto
    {
        [Required, MaxLength(250)]
        public string Outcome { get; set; } = null!;

        // Code is now auto-generated via number series — not user-entered.

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public CBCLevel Level { get; set; }

        public bool IsCore { get; set; } = true;

        [Required]
        public Guid LearningAreaId { get; set; }

        [Required]
        public Guid StrandId { get; set; }

        [Required]
        public Guid SubStrandId { get; set; }

        /// <summary>Required when caller is SuperAdmin; ignored for school users.</summary>
        public Guid? TenantId { get; set; }
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public class UpdateLearningOutcomeDto
    {
        [Required, MaxLength(250)]
        public string Outcome { get; set; } = null!;

        // Code is now auto-generated via number series — not user-entered.

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public CBCLevel Level { get; set; }

        public bool IsCore { get; set; } = true;

        [Required]
        public Guid LearningAreaId { get; set; }

        [Required]
        public Guid StrandId { get; set; }

        [Required]
        public Guid SubStrandId { get; set; }
    }
}
