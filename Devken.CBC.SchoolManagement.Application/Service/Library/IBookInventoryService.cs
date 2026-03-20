using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookInventoryService
    {
        Task<IEnumerable<BookInventoryDto>> GetAllInventoryAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookInventoryDto> GetInventoryByBookAsync(Guid bookId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookInventoryDto> GetInventoryByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookInventoryDto> CreateInventoryAsync(CreateBookInventoryRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookInventoryDto> UpdateInventoryAsync(Guid id, UpdateBookInventoryRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteInventoryAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        /// <summary>
        /// Recalculates all inventory counts from actual BookCopy records for a book.
        /// </summary>
        Task<BookInventoryDto> RecalculateAsync(Guid bookId, Guid? userSchoolId, bool isSuperAdmin);
    }
}