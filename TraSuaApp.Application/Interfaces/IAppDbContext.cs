using TraSuaApp.Domain.Interfaces;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IAppDbContext
{
    IRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
