using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance
{
    /// <summary>
    /// Repository interface for FeeStructure entities.
    /// Extends the generic base with fee-structure-specific query methods.
    /// </summary>
    public interface IFeeStructureRepository : IRepositoryBase<FeeStructure, Guid>
    {
        /// <summary>
        /// Returns all fee structures, optionally scoped to a tenant.
        /// </summary>
        Task<IEnumerable<FeeStructure>> GetAllAsync(
            Guid? tenantId,
            bool trackChanges = false);

        /// <summary>
        /// Returns a single fee structure by its primary key, including navigation properties.
        /// </summary>
        Task<FeeStructure?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Returns all fee structures for a given FeeItem.
        /// </summary>
        Task<IEnumerable<FeeStructure>> GetByFeeItemAsync(
            Guid feeItemId,
            Guid? tenantId,
            bool trackChanges = false);

        /// <summary>
        /// Returns all fee structures for a given academic year.
        /// </summary>
        Task<IEnumerable<FeeStructure>> GetByAcademicYearAsync(
            Guid academicYearId,
            Guid? tenantId,
            bool trackChanges = false);

        /// <summary>
        /// Returns all fee structures for a given term.
        /// </summary>
        Task<IEnumerable<FeeStructure>> GetByTermAsync(
            Guid termId,
            Guid? tenantId,
            bool trackChanges = false);

        /// <summary>
        /// Returns fee structures matching a CBC level (null = annual / all-levels records).
        /// </summary>
        Task<IEnumerable<FeeStructure>> GetByLevelAsync(
            CBCLevel level,
            Guid? tenantId,
            bool trackChanges = false);

        /// <summary>
        /// Checks for a duplicate structure that would conflict on
        /// (TenantId, FeeItemId, AcademicYearId, TermId, Level, ApplicableTo).
        /// Optionally excludes a given id (for update scenarios).
        /// </summary>
        Task<bool> ExistsDuplicateAsync(
            Guid tenantId,
            Guid feeItemId,
            Guid academicYearId,
            Guid? termId,
            CBCLevel? level,
            ApplicableTo applicableTo,
            Guid? excludeId = null);
    }
}