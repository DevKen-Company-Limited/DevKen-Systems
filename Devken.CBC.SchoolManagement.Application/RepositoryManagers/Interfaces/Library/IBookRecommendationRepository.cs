using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookRecommendationRepository : IRepositoryBase<BookRecommendation, Guid>
    {
        /// <summary>
        /// Gets all book recommendations for a tenant
        /// </summary>
        Task<IEnumerable<BookRecommendation>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false);

        /// <summary>
        /// Gets recommendations for a specific student
        /// </summary>
        Task<IEnumerable<BookRecommendation>> GetByStudentIdAsync(Guid tenantId, Guid studentId, bool trackChanges = false);

        /// <summary>
        /// Gets recommendations for a specific book
        /// </summary>
        Task<IEnumerable<BookRecommendation>> GetByBookIdAsync(Guid tenantId, Guid bookId, bool trackChanges = false);

        /// <summary>
        /// Gets top N recommended books for a student ordered by score
        /// </summary>
        Task<IEnumerable<BookRecommendation>> GetTopRecommendationsAsync(Guid tenantId, Guid studentId, int topN = 10, bool trackChanges = false);

        /// <summary>
        /// Checks if a recommendation already exists
        /// </summary>
        Task<bool> ExistsAsync(Guid tenantId, Guid bookId, Guid studentId);

        /// <summary>
        /// Gets recommendation by book and student
        /// </summary>
        Task<BookRecommendation?> GetByBookAndStudentAsync(Guid tenantId, Guid bookId, Guid studentId, bool trackChanges = false);

        /// <summary>
        /// Deletes all recommendations for a specific student
        /// </summary>
        Task DeleteByStudentIdAsync(Guid tenantId, Guid studentId);

        /// <summary>
        /// Deletes all recommendations for a specific book
        /// </summary>
        Task DeleteByBookIdAsync(Guid tenantId, Guid bookId);
    }
}