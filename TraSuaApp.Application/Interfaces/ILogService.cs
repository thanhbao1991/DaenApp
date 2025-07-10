using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface ILogService
{
    Task<List<LogDto>> GetAllAsync();
    Task<List<LogDto>> GetByDateAsync(DateTime ngay);
    Task<List<LogDto>> GetByEntityIdAsync(Guid entityId);
    Task<LogDto?> GetByIdAsync(Guid id);
    Task<Result<LogDto>> CreateAsync(LogDto dto);
    Task<Result<LogDto>> UpdateAsync(Guid id, LogDto dto);
    Task<Result<LogDto>> DeleteAsync(Guid id);
}