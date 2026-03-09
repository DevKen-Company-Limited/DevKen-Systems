using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookAuthorService
    {
        Task<IEnumerable<BookAuthorResponseDto>> GetAllAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookAuthorResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookAuthorResponseDto> CreateAsync(CreateBookAuthorDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookAuthorResponseDto> UpdateAsync(Guid id, UpdateBookAuthorDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}
