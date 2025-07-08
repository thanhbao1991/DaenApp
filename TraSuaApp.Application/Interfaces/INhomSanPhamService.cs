using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface INhomSanPhamService
{
    Task<List<NhomSanPhamDto>> GetAllAsync();
    Task<NhomSanPhamDto?> GetByIdAsync(Guid id);
    Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id);
}