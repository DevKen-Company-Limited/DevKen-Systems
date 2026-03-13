using Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Payments
{
    public sealed class PesaPalTransactionQuery : IPesaPalTransactionQuery
    {
        private readonly AppDbContext _db;
        private readonly TenantContext _tenant;

        public PesaPalTransactionQuery(AppDbContext db, TenantContext tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<PesaPalTransactionPageDto> GetPagedAsync(
            int page,
            int pageSize,
            string? status)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var query = _db.PesaPalTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == _tenant.TenantId);

            // Optional status filter
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var domainStatus))
            {
                query = query.Where(t => t.PaymentStatus == domainStatus);
            }

            var total = await query.CountAsync();

            var rows = await query
                .OrderByDescending(t => t.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new PesaPalTransactionRowDto
                {
                    Id = t.Id.ToString(),
                    OrderTrackingId = t.OrderTrackingId,
                    MerchantReference = t.MerchantReference,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Description = t.Description,
                    PaymentStatus = t.PaymentStatus.ToString().ToUpperInvariant(),
                    PaymentMethod = t.PaymentMethod != null ? t.PaymentMethod.ToString() : null,
                    ConfirmationCode = t.ConfirmationCode,
                    PaymentAccount = t.PaymentAccount,
                    ErrorMessage = t.ErrorMessage,
                    PayerFirstName = t.PayerFirstName,
                    PayerLastName = t.PayerLastName,
                    PayerEmail = t.PayerEmail,
                    PayerPhone = t.PayerPhone,
                    CreatedOn = t.CreatedOn.ToString("o"),
                    UpdatedOn = t.UpdatedOn.ToString("o"),
                    CompletedOn = t.CompletedOn.HasValue ? t.CompletedOn.Value.ToString("o") : null,
                })
                .ToListAsync();

            return new PesaPalTransactionPageDto
            {
                Items = rows,
                Total = total,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}

