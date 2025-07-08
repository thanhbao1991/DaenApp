using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface ISanPhamService
{
    Task<List<SanPhamDto>> GetAllAsync();
    Task<SanPhamDto?> GetByIdAsync(Guid id);
    Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto);
    Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto);
    Task<Result<SanPhamDto>> DeleteAsync(Guid id);
}