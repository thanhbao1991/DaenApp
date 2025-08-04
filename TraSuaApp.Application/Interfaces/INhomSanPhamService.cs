using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface INhomSanPhamService
{
    Task<List<NhomSanPhamDto>> GetAllAsync();
    Task<NhomSanPhamDto?> GetByIdAsync(Guid id);
    Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id);
    Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id);
    Task<List<NhomSanPhamDto>> GetUpdatedSince(DateTime lastSync);
}
