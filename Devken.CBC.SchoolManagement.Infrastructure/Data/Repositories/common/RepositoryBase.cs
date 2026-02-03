using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
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
    /// Changed from internal to public to allow inheritance by public repositories
    /// </summary>
    public class RepositoryBase<T, TId> : IRepositoryBase<T, TId>
            where T : BaseEntity<TId>
            where TId : IEquatable<TId>
    {
        protected readonly AppDbContext _context;
        /// <summary>
        /// Resolved per-request by TenantMiddleware.  We read
        /// ActingUserId from here to stamp CreatedBy / UpdatedBy.
        /// </summary>
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

            // Stamp the acting user.  Null is allowed for bootstrap
            // rows created before any user exists (e.g. Permission seed).
            entity.CreatedBy = _tenantContext.ActingUserId;
            entity.UpdatedBy = _tenantContext.ActingUserId;

            _context.Set<T>().Add(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdatedOn = DateTime.UtcNow;
            entity.UpdatedBy = _tenantContext.ActingUserId;

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