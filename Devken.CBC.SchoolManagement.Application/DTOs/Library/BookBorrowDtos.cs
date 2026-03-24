using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Library
{
    public class BookBorrowDto
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberNumber { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string BorrowStatus { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public int TotalItems { get; set; }
        public int ReturnedItems { get; set; }
        public int UnreturnedItems { get; set; }
        public decimal TotalFines { get; set; }
        public List<BookBorrowItemDto> Items { get; set; } = new List<BookBorrowItemDto>();

        // Multi-tenant fields
        public Guid? SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public Guid? TenantId { get; set; }
    }

    public class BookBorrowItemDto
    {
        public Guid Id { get; set; }
        public Guid BorrowId { get; set; }
        public Guid BookCopyId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public DateTime? ReturnedOn { get; set; }
        public bool IsReturned { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public List<LibraryFineDto> Fines { get; set; } = new List<LibraryFineDto>();
    }

    public class CreateBookBorrowDto
    {
        [Required]
        public Guid MemberId { get; set; }

        [Required]
        public DateTime BorrowDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one book copy must be borrowed")]
        public List<Guid> BookCopyIds { get; set; } = new List<Guid>();

        // SuperAdmin only - if provided, validates access
        public Guid? SchoolId { get; set; }
    }

    public class UpdateBookBorrowDto
    {
        public DateTime? DueDate { get; set; }
    }

    public class ReturnBookDto
    {
        [Required]
        public Guid BorrowItemId { get; set; }

        public DateTime? ReturnDate { get; set; }
    }

    public class ReturnMultipleBooksDto
    {
        [Required]
        [MinLength(1)]
        public List<Guid> BorrowItemIds { get; set; } = new List<Guid>();

        public DateTime? ReturnDate { get; set; }
    }


    //public class LibraryFineDto
    //{
    //    public Guid Id { get; set; }
    //    public Guid BorrowItemId { get; set; }
    //    public decimal Amount { get; set; }
    //    public bool IsPaid { get; set; }
    //    public DateTime IssuedOn { get; set; }
    //    public DateTime? PaidOn { get; set; }
    //    public string? Reason { get; set; }
    //}
}