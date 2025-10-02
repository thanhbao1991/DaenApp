using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ISanPhamService
{
    Task<Result<SanPhamDto>> UpdateSingleAsync(Guid id, SanPhamDto dto);

    Task<Result<SanPhamDto>> RestoreAsync(Guid id);
    Task<List<SanPhamDto>> GetAllAsync();
    Task<SanPhamDto?> GetByIdAsync(Guid id);
    Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto);
    Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto);
    Task<Result<SanPhamDto>> DeleteAsync(Guid id);

    // 🟟 API đồng bộ
    Task<List<SanPhamDto>> GetUpdatedSince(DateTime lastSync);
}
