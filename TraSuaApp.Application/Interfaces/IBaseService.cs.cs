using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IBaseService<TDto>
{
    Task<List<TDto>> GetAllAsync();
    Task<TDto?> GetByIdAsync(Guid id);
    Task<Result<TDto>> CreateAsync(TDto dto);
    Task<Result<TDto>> UpdateAsync(Guid id, TDto dto);
    Task<Result<TDto>> DeleteAsync(Guid id);
    Task<Result<TDto>> RestoreAsync(Guid id);
    Task<List<TDto>> GetUpdatedSince(DateTime lastSync);
}
