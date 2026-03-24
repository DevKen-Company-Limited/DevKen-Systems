using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookBorrowService
    {
        Task<BookBorrowDto> CreateBorrowAsync(CreateBookBorrowDto dto, Guid? userSchoolId);
        Task<BookBorrowDto> GetBorrowByIdAsync(Guid id, Guid? userSchoolId);
        Task<IEnumerable<BookBorrowDto>> GetAllBorrowsAsync(Guid? userSchoolId);
        Task<IEnumerable<BookBorrowDto>> GetBorrowsByMemberIdAsync(Guid memberId, Guid? userSchoolId);
        Task<IEnumerable<BookBorrowDto>> GetActiveBorrowsAsync(Guid? userSchoolId);
        Task<IEnumerable<BookBorrowDto>> GetOverdueBorrowsAsync(Guid? userSchoolId);
        Task<BookBorrowDto> UpdateBorrowAsync(Guid id, UpdateBookBorrowDto dto, Guid? userSchoolId);
        Task<BookBorrowItemDto> ReturnBookAsync(ReturnBookDto dto, Guid? userSchoolId);
        Task<IEnumerable<BookBorrowItemDto>> ReturnMultipleBooksAsync(ReturnMultipleBooksDto dto, Guid? userSchoolId);
        Task DeleteBorrowAsync(Guid id, Guid? userSchoolId);
        Task<bool> CanMemberBorrowAsync(Guid memberId, Guid? userSchoolId);
        Task<int> GetActiveBorrowCountAsync(Guid memberId, Guid? userSchoolId);
        Task ProcessOverdueItemsAsync(Guid? userSchoolId);
    }
}