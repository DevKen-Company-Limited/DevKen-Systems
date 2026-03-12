using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookPublisherService
    {
        Task<IEnumerable<BookPublisherResponseDto>> GetAllAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookPublisherResponseDto> GetByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookPublisherResponseDto> CreateAsync(CreateBookPublisherDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task<BookPublisherResponseDto> UpdateAsync(Guid id, UpdateBookPublisherDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}
