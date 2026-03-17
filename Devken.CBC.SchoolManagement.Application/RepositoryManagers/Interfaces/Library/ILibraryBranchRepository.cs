using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface ILibraryBranchRepository : IRepositoryBase<LibraryBranch, Guid>
    {
        /// <summary>Get all branches with copy counts.</summary>
        Task<IEnumerable<LibraryBranch>> GetAllAsync(bool trackChanges);

        /// <summary>Get all branches for a specific school.</summary>
        Task<IEnumerable<LibraryBranch>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get a branch by ID with all BookCopies included.</summary>
        Task<LibraryBranch?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Check if a branch name already exists for the school (uniqueness).</summary>
        Task<LibraryBranch?> GetByNameAsync(string name, Guid schoolId);
    }
}