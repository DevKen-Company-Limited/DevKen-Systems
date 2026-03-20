using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookCopyService
    {
        Task<IEnumerable<BookCopyDto>> GetAllCopiesAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookCopyDto>> GetCopiesByBookAsync(Guid bookId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookCopyDto>> GetCopiesByBranchAsync(Guid branchId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> GetCopyByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> CreateCopyAsync(CreateBookCopyRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> UpdateCopyAsync(Guid id, UpdateBookCopyRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteCopyAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> MarkAsLostAsync(Guid id, string? remarks, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> MarkAsDamagedAsync(Guid id, string? remarks, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookCopyDto> MarkAsAvailableAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}