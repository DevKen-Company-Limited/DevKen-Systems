using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class CreateBookPublisherDto
    {
        [Required(ErrorMessage = "Publisher name is required.")]
        [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateBookPublisherDto
    {
        [Required(ErrorMessage = "Publisher name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Address { get; set; }
    }

    public class BookPublisherResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public Guid TenantId { get; set; }
        public string? SchoolName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
