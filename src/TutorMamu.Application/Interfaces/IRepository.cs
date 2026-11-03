using System.Linq.Expressions;
using TutorMamu.Domain.Entities;

namespace TutorMamu.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);

    /// <summary>All repositories in a request share one DbContext, so one call saves every pending change.</summary>
    Task SaveChangesAsync();
}
