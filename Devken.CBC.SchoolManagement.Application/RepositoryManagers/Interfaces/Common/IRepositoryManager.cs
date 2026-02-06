using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
{
    public interface IRepositoryManager
    {
        // Academic Repositories
        IStudentRepository Student { get; }
        ISchoolRepository School { get; }

        // Identity Repositories
        IUserRepository User { get; }
        IRoleRepository Role { get; }
        IPermissionRepository Permission { get; }
        IRolePermissionRepository RolePermission { get; }
        IUserRoleRepository UserRole { get; }
        IRefreshTokenRepository RefreshToken { get; }
        ISuperAdminRepository SuperAdmin { get; }

        // Unit of Work
        Task SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}