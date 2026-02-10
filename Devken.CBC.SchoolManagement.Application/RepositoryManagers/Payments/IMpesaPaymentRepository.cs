using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments
{
    public interface IMpesaPaymentRepository : IRepositoryBase<MpesaPaymentRecord, Guid>
    {
        Task<MpesaPaymentRecord?> GetByCheckoutRequestIdAsync(string checkoutRequestId, bool trackChanges = false);
        Task<bool> ExistsByCheckoutRequestIdAsync(string checkoutRequestId);
    }
}
