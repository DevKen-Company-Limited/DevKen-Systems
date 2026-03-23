using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface ILibraryMemberRepository : IRepositoryBase<LibraryMember, Guid>
    {
        /// <summary>Get all members with User navigation included.</summary>
        Task<IEnumerable<LibraryMember>> GetAllAsync(bool trackChanges);

        /// <summary>Get all members for a specific school/tenant.</summary>
        Task<IEnumerable<LibraryMember>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get a member by ID with User and BorrowTransactions included.</summary>
        Task<LibraryMember?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Get a member by their unique MemberNumber within a school.</summary>
        Task<LibraryMember?> GetByMemberNumberAsync(string memberNumber, Guid schoolId);

        /// <summary>Get a member by UserId within a school (one user → one membership per school).</summary>
        Task<LibraryMember?> GetByUserIdAsync(Guid userId, Guid schoolId);
    }
}