using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Tenant
{
    public class SchoolDto
    {
        public Guid Id { get; set; }
        public string SlugName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class CreateSchoolRequest
    {
        [Required]
        public string SlugName { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class UpdateSchoolRequest
    {
        [Required]
        public string SlugName { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
