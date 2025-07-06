using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface ISanPhamService
{
    Task<List<SanPhamDto>> GetAllAsync();
    Task<SanPhamDto?> GetByIdAsync(Guid id);
    Task<Result> CreateAsync(SanPhamDto dto);
    Task<Result> UpdateAsync(Guid id, SanPhamDto dto);
    Task<Result> DeleteAsync(Guid id);
}