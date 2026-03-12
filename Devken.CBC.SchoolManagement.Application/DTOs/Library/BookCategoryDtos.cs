using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class CreateBookCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Required only when called by SuperAdmin.</summary>
        public Guid? TenantId { get; set; }
    }

    public class UpdateBookCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class BookCategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid TenantId { get; set; }
        public string? SchoolName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}