using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Payments
{
    public class PaymentRepository
        : RepositoryBase<Payment, Guid>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }
    }
}
