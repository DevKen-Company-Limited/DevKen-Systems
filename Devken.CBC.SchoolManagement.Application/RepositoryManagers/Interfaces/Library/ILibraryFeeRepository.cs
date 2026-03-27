using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface ILibraryFeeRepository : IRepositoryBase<LibraryFee, Guid>
    {
        /// <summary>Gets all fees across all schools (SuperAdmin only).</summary>
        Task<IEnumerable<LibraryFee>> GetAllAsync(bool trackChanges);

        /// <summary>Gets all fees for a specific school.</summary>
        Task<IEnumerable<LibraryFee>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Gets all fees for a specific library member.</summary>
        Task<IEnumerable<LibraryFee>> GetByMemberIdAsync(Guid memberId, Guid schoolId, bool trackChanges);

        /// <summary>Gets all fees linked to a specific borrow transaction.</summary>
        Task<IEnumerable<LibraryFee>> GetByBorrowIdAsync(Guid borrowId, bool trackChanges);

        /// <summary>Gets a fee by ID with Member and BookBorrow navigation included.</summary>
        Task<LibraryFee?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>Gets all fees filtered by status for a school.</summary>
        Task<IEnumerable<LibraryFee>> GetByStatusAsync(
            Guid schoolId, LibraryFeeStatus status, bool trackChanges);

        /// <summary>Gets filtered fees by multiple optional criteria.</summary>
        Task<IEnumerable<LibraryFee>> GetFilteredAsync(
            Guid? schoolId,
            Guid? memberId,
            LibraryFeeStatus? status,
            LibraryFeeType? feeType,
            DateTime? fromDate,
            DateTime? toDate,
            bool trackChanges);

        /// <summary>Gets the total outstanding balance for a member at a school.</summary>
        Task<decimal> GetOutstandingBalanceAsync(Guid memberId, Guid schoolId);
    }
}