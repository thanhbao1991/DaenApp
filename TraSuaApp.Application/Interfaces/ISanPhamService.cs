using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface ISanPhamService
{
    Task<List<SanPhamDto>> GetAllAsync();
    Task<SanPhamDto?> GetByIdAsync(Guid id);
    Task<SanPhamDto> CreateAsync(SanPhamDto dto);
    Task<SanPhamDto> UpdateAsync(Guid id, SanPhamDto dto);
    Task<bool> DeleteAsync(Guid id);
}