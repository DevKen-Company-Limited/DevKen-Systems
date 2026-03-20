using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookCopyRepository : IRepositoryBase<BookCopy, Guid>
    {
        /// <summary>Get all copies with Book and LibraryBranch navigation.</summary>
        Task<IEnumerable<BookCopy>> GetAllAsync(bool trackChanges);

        /// <summary>Get all copies for a specific school.</summary>
        Task<IEnumerable<BookCopy>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get all copies for a specific book.</summary>
        Task<IEnumerable<BookCopy>> GetByBookIdAsync(Guid bookId, bool trackChanges);

        /// <summary>Get all copies assigned to a specific library branch.</summary>
        Task<IEnumerable<BookCopy>> GetByBranchIdAsync(Guid branchId, bool trackChanges);

        /// <summary>Get a copy by ID with full navigation properties.</summary>
        Task<BookCopy?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Check if accession number already exists within a school.</summary>
        Task<BookCopy?> GetByAccessionNumberAsync(string accessionNumber, Guid schoolId);

        /// <summary>Check if barcode already exists within a school.</summary>
        Task<BookCopy?> GetByBarcodeAsync(string barcode, Guid schoolId);

        /// <summary>Get all available copies for a book.</summary>
        Task<IEnumerable<BookCopy>> GetAvailableCopiesByBookAsync(Guid bookId);

        /// <summary>Count copies by book and availability state.</summary>
        Task<int> CountByBookIdAsync(Guid bookId);
        Task<int> CountAvailableByBookIdAsync(Guid bookId);
        Task<int> CountLostByBookIdAsync(Guid bookId);
        Task<int> CountDamagedByBookIdAsync(Guid bookId);
    }
}