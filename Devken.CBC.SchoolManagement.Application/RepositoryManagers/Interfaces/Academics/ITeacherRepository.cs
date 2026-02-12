using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics
{
    public interface ITeacherRepository : IRepositoryBase<Teacher, Guid>
    {
        Task<IEnumerable<Teacher>> GetAllAsync(bool trackChanges);
        Task<IEnumerable<Teacher>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);
        Task<Teacher?> GetByTeacherNumberAsync(string teacherNumber, Guid schoolId);
        Task<Teacher?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);
    }
}
