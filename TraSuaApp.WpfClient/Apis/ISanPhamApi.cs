using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ISanPhamApi
{
    Task<Result<List<SanPhamDto>>> GetAllAsync();
    Task<Result<SanPhamDto>> GetByIdAsync(Guid id);
    Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto);
    Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto);
    Task<Result<SanPhamDto>> DeleteAsync(Guid id);
    Task<Result<SanPhamDto>> RestoreAsync(Guid id);
    Task<Result<List<SanPhamDto>>> GetUpdatedSince(DateTime since);
}
