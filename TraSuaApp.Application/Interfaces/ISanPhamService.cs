using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface ISanPhamService
{
    Task<List<SanPhamDto>> GetAllAsync();
    Task<SanPhamDto?> GetByIdAsync(Guid id);
    Task CreateAsync(SanPhamDto dto);
    Task UpdateAsync(Guid id, SanPhamDto dto);
    Task DeleteAsync(Guid id);
}