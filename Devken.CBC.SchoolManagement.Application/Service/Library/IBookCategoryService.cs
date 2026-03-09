using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookCategoryService
    {
        Task<IEnumerable<BookCategoryResponseDto>> GetAllAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookCategoryResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookCategoryResponseDto> CreateAsync(CreateBookCategoryDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookCategoryResponseDto> UpdateAsync(Guid id, UpdateBookCategoryDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}
