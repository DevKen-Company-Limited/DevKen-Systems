// Application/DTOs/Library/LibrarySettingsDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class LibrarySettingsDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public int MaxBooksPerStudent { get; set; }
        public int MaxBooksPerTeacher { get; set; }
        public int BorrowDaysStudent { get; set; }
        public int BorrowDaysTeacher { get; set; }
        public decimal FinePerDay { get; set; }
        public bool AllowBookReservation { get; set; }
    }

    public class UpsertLibrarySettingsRequest
    {
        /// <summary>Required for SuperAdmin only.</summary>
        public Guid? SchoolId { get; set; }

        [Range(1, 50)]
        public int MaxBooksPerStudent { get; set; } = 2;

        [Range(1, 50)]
        public int MaxBooksPerTeacher { get; set; } = 5;

        [Range(1, 365)]
        public int BorrowDaysStudent { get; set; } = 7;

        [Range(1, 365)]
        public int BorrowDaysTeacher { get; set; } = 14;

        [Range(0, 10000)]
        public decimal FinePerDay { get; set; } = 10;

        public bool AllowBookReservation { get; set; } = true;
    }
}