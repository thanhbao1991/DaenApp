using TraSuaApp.Domain.Interfaces;

namespace TraSuaApp.Application.Interfaces;

public interface IAppDbContext
{
    IRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}