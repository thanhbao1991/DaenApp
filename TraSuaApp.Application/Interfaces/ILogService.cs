
using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Application.Interfaces;

public interface ILogService
{
    Task LogAsync(LogEntry log, CancellationToken cancellationToken = default);
}