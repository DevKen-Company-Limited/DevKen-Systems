using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Activities
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(Guid userId, Guid? tenantId, string activityType, string? details = null);
    }
}
