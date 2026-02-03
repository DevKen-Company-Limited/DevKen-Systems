using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Activities
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(Guid userId, Guid? tenantId, string activityType, string? details = null);
    }
}
