using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IToppingService
{
    Task<List<ToppingDto>> GetAllAsync();
    Task<ToppingDto?> GetByIdAsync(Guid id);
    Task<Result<ToppingDto>> CreateAsync(ToppingDto dto);
    Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto);
    Task<Result<ToppingDto>> DeleteAsync(Guid id);
    Task<Result<ToppingDto>> RestoreAsync(Guid id);
    Task<List<ToppingDto>> GetUpdatedSince(DateTime lastSync);
}
