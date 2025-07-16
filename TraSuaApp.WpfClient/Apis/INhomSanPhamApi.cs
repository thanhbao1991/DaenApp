using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface INhomSanPhamApi
{
    Task<Result<List<NhomSanPhamDto>>> GetAllAsync();
    Task<Result<NhomSanPhamDto>> GetByIdAsync(Guid id);
    Task<Result<List<NhomSanPhamDto>>> GetUpdatedSince(DateTime since);
    Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto);
    Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id);
    Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id);
}