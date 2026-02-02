using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly ILogger<RepositoryManager> _logger;
        private readonly AppDbContext _context;

        private readonly Lazy<ISchoolRepository> _schoolRepository;
        private readonly Lazy<IUserRepository> _userRepository;
        private readonly Lazy<IRoleRepository> _roleRepository;
        private readonly Lazy<IPermissionRepository> _permissionRepository;
        private readonly Lazy<IRolePermissionRepository> _rolePermissionRepository;
        private readonly Lazy<IUserRoleRepository> _userRoleRepository;
        private readonly Lazy<IRefreshTokenRepository> _refreshTokenRepository;
        private readonly Lazy<ISuperAdminRepository> _superAdminRepository;

        public RepositoryManager(
            ILogger<RepositoryManager> logger,
            AppDbContext context,
            Lazy<ISchoolRepository> schoolRepository,
            Lazy<IUserRepository> userRepository,
            Lazy<IRoleRepository> roleRepository,
            Lazy<IPermissionRepository> permissionRepository,
            Lazy<IRolePermissionRepository> rolePermissionRepository,
            Lazy<IUserRoleRepository> userRoleRepository,
            Lazy<IRefreshTokenRepository> refreshTokenRepository,
            Lazy<ISuperAdminRepository> superAdminRepository)
        {
            _logger = logger;
            _context = context;
            _schoolRepository = schoolRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userRoleRepository = userRoleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _superAdminRepository = superAdminRepository;
        }

        public ISchoolRepository School => _schoolRepository.Value;
        public IUserRepository User => _userRepository.Value;
        public IRoleRepository Role => _roleRepository.Value;
        public IPermissionRepository Permission => _permissionRepository.Value;
        public IRolePermissionRepository RolePermission => _rolePermissionRepository.Value;
        public IUserRoleRepository UserRole => _userRoleRepository.Value;
        public IRefreshTokenRepository RefreshToken => _refreshTokenRepository.Value;
        public ISuperAdminRepository SuperAdmin => _superAdminRepository.Value;

        public Task SaveAsync()
        {
            _logger.LogDebug("RepositoryManager.SaveAsync");
            return _context.SaveChangesAsync();
        }
    }
}
