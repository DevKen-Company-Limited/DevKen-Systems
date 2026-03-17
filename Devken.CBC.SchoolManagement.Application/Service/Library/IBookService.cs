using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookService
    {
        Task<IEnumerable<BookDto>> GetAllBooksAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookDto>> GetBooksByCategoryAsync(Guid categoryId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(Guid authorId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookDto> GetBookByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookDto> CreateBookAsync(CreateBookRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookDto> UpdateBookAsync(Guid id, UpdateBookRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteBookAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}