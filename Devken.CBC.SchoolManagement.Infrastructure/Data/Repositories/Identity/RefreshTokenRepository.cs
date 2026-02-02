using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity
{
    internal class RefreshTokenRepository
          : RepositoryBase<RefreshToken, Guid>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<RefreshToken?> GetByTokenStringAsync(string token) =>
            await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId) =>
            await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

        public void RevokeAllByUserId(Guid userId)
        {
            var tokens = _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToList();

            var now = DateTime.UtcNow;
            foreach (var t in tokens)
                t.RevokedAt = now;
        }
    }

}
