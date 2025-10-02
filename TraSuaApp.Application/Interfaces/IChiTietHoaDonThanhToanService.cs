using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IChiTietHoaDonThanhToanService
{
    Task<List<ChiTietHoaDonThanhToanDto>> GetAllAsync();
    Task<ChiTietHoaDonThanhToanDto?> GetByIdAsync(Guid id);
    Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto);
    Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto);
    Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id);
    Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id);
    Task<List<ChiTietHoaDonThanhToanDto>> GetUpdatedSince(DateTime lastSync);
}
