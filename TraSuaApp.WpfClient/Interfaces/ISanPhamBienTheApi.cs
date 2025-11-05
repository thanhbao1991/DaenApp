using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ISanPhamBienTheApi
{
    Task<Result<List<SanPhamBienTheDto>>> GetAllAsync();
    Task<Result<SanPhamBienTheDto>> GetByIdAsync(Guid id);
    Task<Result<SanPhamBienTheDto>> CreateAsync(SanPhamBienTheDto dto);
    Task<Result<SanPhamBienTheDto>> UpdateAsync(Guid id, SanPhamBienTheDto dto);
    Task<Result<SanPhamBienTheDto>> DeleteAsync(Guid id);
    Task<Result<SanPhamBienTheDto>> RestoreAsync(Guid id);
    Task<Result<List<SanPhamBienTheDto>>> GetUpdatedSince(DateTime since);
}
