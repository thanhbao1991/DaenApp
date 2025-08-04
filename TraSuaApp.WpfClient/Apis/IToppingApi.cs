using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IToppingApi
{
    Task<Result<List<ToppingDto>>> GetAllAsync();
    Task<Result<ToppingDto>> GetByIdAsync(Guid id);
    Task<Result<List<ToppingDto>>> GetUpdatedSince(DateTime since);
    Task<Result<ToppingDto>> CreateAsync(ToppingDto dto);
    Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto);
    Task<Result<ToppingDto>> DeleteAsync(Guid id);
    Task<Result<ToppingDto>> RestoreAsync(Guid id);
}
