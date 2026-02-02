using Devken.CBC.SchoolManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
{
    public interface IRepositoryBase<T, TId>
        where T : BaseEntity<TId>
        where TId : IEquatable<TId>
    {
        IQueryable<T> FindAll(bool trackChanges);
        IQueryable<T> FindByCondition(
            Expression<Func<T, bool>> expression, bool trackChanges);

        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);

        T? GetById(TId id);
        Task<T?> GetByIdAsync(TId id, bool trackChanges = false);
        Task<bool> ExistAsync(TId id);
    }
}
