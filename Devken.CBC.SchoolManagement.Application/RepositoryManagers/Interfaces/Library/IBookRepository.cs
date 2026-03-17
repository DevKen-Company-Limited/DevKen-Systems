using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookRepository : IRepositoryBase<Book, Guid>
    {
        /// <summary>Get all books with navigation properties.</summary>
        Task<IEnumerable<Book>> GetAllAsync(bool trackChanges);

        /// <summary>Get all books for a specific school.</summary>
        Task<IEnumerable<Book>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get a book by ID including copies, author, category, publisher.</summary>
        Task<Book?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Look up a book by ISBN within a school (for uniqueness checks).</summary>
        Task<Book?> GetByISBNAsync(string isbn, Guid schoolId);

        /// <summary>Get all books in a given category for a school.</summary>
        Task<IEnumerable<Book>> GetByCategoryAsync(Guid categoryId, Guid schoolId, bool trackChanges);

        /// <summary>Get all books by a given author for a school.</summary>
        Task<IEnumerable<Book>> GetByAuthorAsync(Guid authorId, Guid schoolId, bool trackChanges);
    }
}