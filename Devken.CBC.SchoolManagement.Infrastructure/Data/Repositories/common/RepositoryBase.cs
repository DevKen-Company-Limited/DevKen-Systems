using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common
{
    /// <summary>
    /// Base repository implementation for all entities
    /// UPDATED: Handles SuperAdmin vs User table separation
    /// </summary>
    public class RepositoryBase<T, TId> : IRepositoryBase<T, TId>
            where T : BaseEntity<TId>
            where TId : IEquatable<TId>
    {
        protected readonly AppDbContext _context;
        protected readonly TenantContext _tenantContext;

        public RepositoryBase(AppDbContext context, TenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ── Queries ──────────────────────────────────────────
        public IQueryable<T> FindAll(bool trackChanges) =>
            !trackChanges
                ? _context.Set<T>().Where(s => s.Status != EntityStatus.Deleted).AsNoTracking()
                : _context.Set<T>().Where(s => s.Status != EntityStatus.Deleted);

        public IQueryable<T> FindByCondition(
            Expression<Func<T, bool>> expression, bool trackChanges) =>
            !trackChanges
                ? _context.Set<T>()
                    .Where(s => s.Status != EntityStatus.Deleted)
                    .Where(expression).AsNoTracking()
                : _context.Set<T>()
                    .Where(s => s.Status != EntityStatus.Deleted)
                    .Where(expression);

        // ── CUD ──────────────────────────────────────────────
        public virtual void Create(T entity)
        {
            var now = DateTime.UtcNow;
            entity.CreatedOn = now;
            entity.UpdatedOn = now;
            entity.Status = EntityStatus.Active;

            // CRITICAL FIX: Handle SuperAdmin creating Users
            // SuperAdmins exist in SuperAdmins table, not Users table
            // So we can't set CreatedBy to SuperAdmin ID for User entities

            var actingUserId = _tenantContext.ActingUserId;
            var isSuperAdmin = _tenantContext.IsSuperAdmin;

            // If a SuperAdmin is creating a User entity, leave CreatedBy/UpdatedBy as null
            // to avoid foreign key constraint violation
            if (entity is User && isSuperAdmin)
            {
                // SuperAdmin creating a user - don't set CreatedBy
                // because SuperAdmin ID exists in SuperAdmins table, not Users table
                entity.CreatedBy = null;
                entity.UpdatedBy = null;
            }
            else
            {
                // Normal case: user creating entity, or non-User entity
                entity.CreatedBy = actingUserId;
                entity.UpdatedBy = actingUserId;
            }

            _context.Set<T>().Add(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdatedOn = DateTime.UtcNow;

            // CRITICAL FIX: Handle SuperAdmin updating Users
            var isSuperAdmin = _tenantContext.IsSuperAdmin;

            if (entity is User && isSuperAdmin)
            {
                // SuperAdmin updating a user - don't set UpdatedBy
                entity.UpdatedBy = null;
            }
            else
            {
                entity.UpdatedBy = _tenantContext.ActingUserId;
            }

            _context.Set<T>().Update(entity);

            // These columns are write-once – never let an Update overwrite them.
            _context.Entry(entity).Property(x => x.CreatedOn).IsModified = false;
            _context.Entry(entity).Property(x => x.CreatedBy).IsModified = false;
            _context.Entry(entity).Property(x => x.Status).IsModified = false;
        }

        public virtual void Delete(T entity) =>
            _context.Set<T>().Remove(entity);

        // ── Lookups ──────────────────────────────────────────
        public T? GetById(TId id) =>
            _context.Set<T>().Find(id);

        public async Task<T?> GetByIdAsync(TId id, bool trackChanges = false)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (!trackChanges && entity != null)
                _context.Entry(entity).State = EntityState.Detached;
            return entity;
        }

        public Task<bool> ExistAsync(TId id) =>
            _context.Set<T>().AsNoTracking().AnyAsync(s => s.Id.Equals(id));
    }
}