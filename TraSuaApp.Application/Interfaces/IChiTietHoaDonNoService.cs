using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IChiTietHoaDonNoService
{
    Task<List<ChiTietHoaDonNoDto>> GetAllAsync();
    Task<ChiTietHoaDonNoDto?> GetByIdAsync(Guid id);
    Task<Result<ChiTietHoaDonNoDto>> CreateAsync(ChiTietHoaDonNoDto dto);
    Task<Result<ChiTietHoaDonNoDto>> UpdateAsync(Guid id, ChiTietHoaDonNoDto dto);
    Task<Result<ChiTietHoaDonNoDto>> DeleteAsync(Guid id);
    Task<Result<ChiTietHoaDonNoDto>> RestoreAsync(Guid id);
    Task<List<ChiTietHoaDonNoDto>> GetUpdatedSince(DateTime lastSync);
}
