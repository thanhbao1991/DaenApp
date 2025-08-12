using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IChiTietHoaDonThanhToanApi
{
    Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetAllAsync();
    Task<Result<ChiTietHoaDonThanhToanDto>> GetByIdAsync(Guid id);
    Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetUpdatedSince(DateTime since);
    Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto);
    Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto);
    Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id);
    Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id);
}
