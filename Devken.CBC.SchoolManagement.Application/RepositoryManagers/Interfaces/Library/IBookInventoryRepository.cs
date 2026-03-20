using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookInventoryRepository : IRepositoryBase<BookInventory, Guid>
    {
        /// <summary>Get all inventory records with Book navigation.</summary>
        Task<IEnumerable<BookInventory>> GetAllAsync(bool trackChanges);

        /// <summary>Get all inventory records for a specific school.</summary>
        Task<IEnumerable<BookInventory>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get the inventory record for a specific book.</summary>
        Task<BookInventory?> GetByBookIdAsync(Guid bookId, bool trackChanges);

        /// <summary>Get an inventory record by ID with full Book navigation.</summary>
        Task<BookInventory?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Check if an inventory record already exists for a book.</summary>
        Task<bool> ExistsByBookIdAsync(Guid bookId);
    }
}