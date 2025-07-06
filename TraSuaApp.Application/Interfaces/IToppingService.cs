using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface IToppingService
{
    Task<List<ToppingDto>> GetAllAsync();
    Task<ToppingDto?> GetByIdAsync(Guid id);
    Task<ToppingDto> CreateAsync(ToppingDto dto);
    Task<bool> UpdateAsync(Guid id, ToppingDto dto);
    Task<bool> DeleteAsync(Guid id);
}