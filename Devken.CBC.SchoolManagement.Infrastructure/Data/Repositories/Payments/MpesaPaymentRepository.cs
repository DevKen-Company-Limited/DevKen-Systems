using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Payments
{
    public class MpesaPaymentRepository
        : RepositoryBase<MpesaPaymentRecord, Guid>, IMpesaPaymentRepository
    {
        public MpesaPaymentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<MpesaPaymentRecord?> GetByCheckoutRequestIdAsync(string checkoutRequestId, bool trackChanges = false)
        {
            return await FindByCondition(x => x.CheckoutRequestId == checkoutRequestId, trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsByCheckoutRequestIdAsync(string checkoutRequestId)
        {
            return await FindByCondition(x => x.CheckoutRequestId == checkoutRequestId, trackChanges: false)
                .AnyAsync();
        }
    }
}
