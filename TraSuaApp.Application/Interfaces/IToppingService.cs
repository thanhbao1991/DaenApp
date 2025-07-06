using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IToppingService
{
    Task<List<ToppingDto>> GetAllAsync();
    Task<ToppingDto?> GetByIdAsync(Guid id);
    Task<Result> CreateAsync(ToppingDto dto);
    Task<Result> UpdateAsync(Guid id, ToppingDto dto);
    Task<Result> DeleteAsync(Guid id);
}