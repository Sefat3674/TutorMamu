using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Infrastructure.Data;

namespace TutorMamu.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _db;

    public Repository(ApplicationDbContext db) => _db = db;

    public async Task<T?> GetByIdAsync(Guid id) => await _db.Set<T>().FindAsync(id);

    public async Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
    {
        IQueryable<T> query = _db.Set<T>();
        if (predicate != null) query = query.Where(predicate);
        return await query.ToListAsync();
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => _db.Set<T>().AnyAsync(predicate);

    public async Task AddAsync(T entity) => await _db.Set<T>().AddAsync(entity);

    public void Update(T entity) => _db.Set<T>().Update(entity);

    public void Remove(T entity) => _db.Set<T>().Remove(entity);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
