using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum
{
    public interface ILearningOutcomeRepository : IRepositoryBase<LearningOutcome, Guid>
    {
        Task<IEnumerable<LearningOutcome>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<LearningOutcome>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<IEnumerable<LearningOutcome>> GetBySubStrandAsync(Guid subStrandId, bool trackChanges);
        Task<IEnumerable<LearningOutcome>> GetByStrandAsync(Guid strandId, bool trackChanges);
        Task<IEnumerable<LearningOutcome>> GetByLearningAreaAsync(Guid learningAreaId, bool trackChanges);
        Task<LearningOutcome?> GetByCodeAsync(string code, Guid tenantId);
        Task<bool> ExistsByCodeAsync(string code, Guid tenantId, Guid? excludeId = null);
        Task<LearningOutcome?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}
