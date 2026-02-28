using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic
{
    public interface IParentRepository : IRepositoryBase<Parent, Guid>
    {
        /// <summary>
        /// Returns all parents for a tenant with Students eagerly loaded.
        /// Filtering and sorting is handled in the service layer.
        /// </summary>
        Task<IEnumerable<Parent>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);

        /// <summary>
        /// Returns a parent with their linked Students eagerly loaded.
        /// </summary>
        Task<Parent?> GetWithStudentsAsync(Guid id, bool trackChanges);

        /// <summary>
        /// Returns all parents linked to a specific student.
        /// </summary>
        Task<IEnumerable<Parent>> GetByStudentIdAsync(Guid studentId, Guid tenantId, bool trackChanges);

        /// <summary>
        /// Checks whether a National ID is already in use within a tenant,
        /// optionally excluding the parent being updated.
        /// </summary>
        Task<bool> NationalIdExistsAsync(string nationalId, Guid tenantId, Guid? excludeId = null);
    }
}

