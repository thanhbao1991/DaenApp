using TraSuaApp.Application.Interfaces;

namespace TraSuaApp.Infrastructure.Services;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;

public class LogService : ILogService
{
    private readonly AppDbContext _dbContext;

    public LogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(LogEntry log, CancellationToken cancellationToken = default)
    {
        _dbContext.Logs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}