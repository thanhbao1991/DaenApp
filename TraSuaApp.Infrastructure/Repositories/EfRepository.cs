using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Interfaces;

namespace TraSuaApp.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _set;

    public EfRepository(DbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public IQueryable<T> All()
        => _set.AsQueryable();

    public async Task<T?> FindAsync(Guid id)
        => await _set.FindAsync(id);

    public void Add(T entity)
        => _set.Add(entity);

    public void Update(T entity)
        => _set.Update(entity);

    public void Remove(T entity)
        => _set.Remove(entity);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => await _set.AnyAsync(predicate);
}
