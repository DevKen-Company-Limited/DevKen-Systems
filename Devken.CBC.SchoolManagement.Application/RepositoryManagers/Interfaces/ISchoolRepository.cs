using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces
{
    public interface ISchoolRepository
        : IRepositoryBase<School, Guid>
    {
        /// <summary>Look up a school by its URL slug.</summary>
        Task<School?> GetBySlugAsync(string slug);
    }
}
