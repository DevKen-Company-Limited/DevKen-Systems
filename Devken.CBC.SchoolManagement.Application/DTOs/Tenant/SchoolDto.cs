using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Tenant
{
    // ─────────────────────────────────────────────────────────────────────────────
    // READ DTO
    // ─────────────────────────────────────────────────────────────────────────────

    public class SchoolDto
    {
        public Guid Id { get; set; }
        public string SlugName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? RegistrationNumber { get; set; }
        public string? KnecCenterCode { get; set; }
        public string? KraPin { get; set; }
        public string? Address { get; set; }
        public string? County { get; set; }
        public string? SubCounty { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public SchoolType SchoolType { get; set; }
        public SchoolCategory Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CREATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────────

    public class CreateSchoolRequest
    {
        [Required(ErrorMessage = "Slug name is required.")]
        [MaxLength(100, ErrorMessage = "Slug name cannot exceed 100 characters.")]
        public string SlugName { get; set; } = null!;

        [Required(ErrorMessage = "School name is required.")]
        [MaxLength(200, ErrorMessage = "School name cannot exceed 200 characters.")]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(50)]
        public string? KnecCenterCode { get; set; }

        [MaxLength(50)]
        public string? KraPin { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(100)]
        public string? SubCounty { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// Optional: provide a direct URL. If a file is uploaded via /logo endpoint, this is ignored.
        /// </summary>
        public string? LogoUrl { get; set; }

        public SchoolType SchoolType { get; set; } = SchoolType.Private;

        public SchoolCategory Category { get; set; } = SchoolCategory.Day;

        public bool IsActive { get; set; } = true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // UPDATE REQUEST
    // ─────────────────────────────────────────────────────────────────────────────

    public class UpdateSchoolRequest
    {
        [Required(ErrorMessage = "Slug name is required.")]
        [MaxLength(100)]
        public string SlugName { get; set; } = null!;

        [Required(ErrorMessage = "School name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(50)]
        public string? KnecCenterCode { get; set; }

        [MaxLength(50)]
        public string? KraPin { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(100)]
        public string? SubCounty { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// Optional direct URL. If a file is uploaded via /logo endpoint, this is ignored.
        /// </summary>
        public string? LogoUrl { get; set; }

        public SchoolType SchoolType { get; set; }

        public SchoolCategory Category { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // PATCH STATUS REQUEST
    // ─────────────────────────────────────────────────────────────────────────────

    public class UpdateSchoolStatusRequest
    {
        public bool IsActive { get; set; }
    }
}