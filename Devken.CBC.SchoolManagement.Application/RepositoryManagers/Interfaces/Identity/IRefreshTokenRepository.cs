using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity
{
    public interface IRefreshTokenRepository
        : IRepositoryBase<RefreshToken, Guid>
    {
        Task<RefreshToken?> GetByTokenStringAsync(string token);
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);
        void RevokeAllByUserId(Guid userId);
    }
}
