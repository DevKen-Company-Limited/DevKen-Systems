using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments
{
    /// <summary>
    /// Allows the Settings UI to persist PesaPal configuration changes
    /// without restarting the host.
    /// </summary>
    public interface IWritablePesaPalSettings
    {
        /// <summary>
        /// Applies <paramref name="update"/> to the current settings and
        /// persists the result to the underlying store.
        /// </summary>
        Task UpdateAsync(Action<PesaPalSettings> update);
    }

}
