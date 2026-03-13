using Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments
{
    /// <summary>
    /// Read-side query for the PesaPal transaction log.
    /// Separated from IPesaPalService so the controller can inject it
    /// without pulling in all of IPesaPalService's PesaPal API methods.
    /// </summary>
    public interface IPesaPalTransactionQuery
    {
        /// <summary>
        /// Returns a paged list of PesaPal transactions for the current tenant.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Rows per page (max 100).</param>
        /// <param name="status">Optional filter: PENDING | COMPLETED | FAILED | REVERSED.</param>
        Task<PesaPalTransactionPageDto> GetPagedAsync(int page, int pageSize, string? status);
    }

}
