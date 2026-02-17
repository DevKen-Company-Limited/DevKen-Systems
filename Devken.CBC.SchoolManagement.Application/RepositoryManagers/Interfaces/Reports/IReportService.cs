using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports
{
    public interface IReportService
    {
        Task<byte[]> GenerateStudentsListReportAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);
    }


}
