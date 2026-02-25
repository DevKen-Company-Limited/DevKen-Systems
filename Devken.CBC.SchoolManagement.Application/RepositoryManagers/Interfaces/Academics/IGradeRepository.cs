using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface IGradeRepository : IRepositoryBase<Grade, Guid>
    {
        Task<IEnumerable<Grade>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<Grade>> GetByTenantIdAsync(Guid tenantId, bool trackChanges);
        Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId, Guid tenantId, bool trackChanges);
        Task<IEnumerable<Grade>> GetBySubjectIdAsync(Guid subjectId, Guid tenantId, bool trackChanges);
        Task<IEnumerable<Grade>> GetByTermIdAsync(Guid termId, Guid tenantId, bool trackChanges);
        Task<Grade?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
        Task<bool> ExistsByStudentSubjectTermAsync(Guid studentId, Guid subjectId, Guid? termId, Guid? excludeId = null);
    }
}