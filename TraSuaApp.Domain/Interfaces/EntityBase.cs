using System.Linq.Expressions;

namespace TraSuaApp.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> All();
    Task<T?> FindAsync(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}