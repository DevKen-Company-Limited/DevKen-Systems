using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly AppDbContext _context;
        private readonly TenantContext _tenantContext;

        // Lazy Repositories - Academic
        private readonly Lazy<IStudentRepository> _studentRepository;
        private readonly Lazy<ISchoolRepository> _schoolRepository;

        // Lazy Repositories - Identity
        private readonly Lazy<IUserRepository> _userRepository;
        private readonly Lazy<IRoleRepository> _roleRepository;
        private readonly Lazy<IPermissionRepository> _permissionRepository;
        private readonly Lazy<IRolePermissionRepository> _rolePermissionRepository;
        private readonly Lazy<IUserRoleRepository> _userRoleRepository;
        private readonly Lazy<IRefreshTokenRepository> _refreshTokenRepository;
        private readonly Lazy<ISuperAdminRepository> _superAdminRepository;

        public RepositoryManager(AppDbContext context, TenantContext tenantContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));

            // Initialize Academic Repositories
            _studentRepository = new Lazy<IStudentRepository>(() =>
                new StudentRepository(_context, (Application.Service.ICurrentUserService)_tenantContext));

            _schoolRepository = new Lazy<ISchoolRepository>(() =>
                new SchoolRepository(_context, _tenantContext));

            // Initialize Identity Repositories
            _userRepository = new Lazy<IUserRepository>(() =>
                new UserRepository(_context, _tenantContext));

            _roleRepository = new Lazy<IRoleRepository>(() =>
                new RoleRepository(_context, _tenantContext));

            _permissionRepository = new Lazy<IPermissionRepository>(() =>
                new PermissionRepository(_context, _tenantContext));

            _rolePermissionRepository = new Lazy<IRolePermissionRepository>(() =>
                new RolePermissionRepository(_context, _tenantContext));

            _userRoleRepository = new Lazy<IUserRoleRepository>(() =>
                new UserRoleRepository(_context, _tenantContext));

            _refreshTokenRepository = new Lazy<IRefreshTokenRepository>(() =>
                new RefreshTokenRepository(_context, _tenantContext));

            _superAdminRepository = new Lazy<ISuperAdminRepository>(() =>
                new SuperAdminRepository(_context, _tenantContext));
        }

        // Academic Repository Properties
        public IStudentRepository Student => _studentRepository.Value;
        public ISchoolRepository School => _schoolRepository.Value;

        // Identity Repository Properties
        public IUserRepository User => _userRepository.Value;
        public IRoleRepository Role => _roleRepository.Value;
        public IPermissionRepository Permission => _permissionRepository.Value;
        public IRolePermissionRepository RolePermission => _rolePermissionRepository.Value;
        public IUserRoleRepository UserRole => _userRoleRepository.Value;
        public IRefreshTokenRepository RefreshToken => _refreshTokenRepository.Value;
        public ISuperAdminRepository SuperAdmin => _superAdminRepository.Value;

        // Unit of Work Methods
        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync() =>
            await _context.Database.BeginTransactionAsync();
    }
}
