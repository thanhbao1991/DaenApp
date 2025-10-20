using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IChiTietHoaDonNoApi
{
    Task<Result<ChiTietHoaDonThanhToanDto>> PayAsync(Guid id, string type);
    Task<Result<List<ChiTietHoaDonNoDto>>> GetAllAsync();
    Task<Result<ChiTietHoaDonNoDto>> GetByIdAsync(Guid id);
    Task<Result<List<ChiTietHoaDonNoDto>>> GetUpdatedSince(DateTime since);
    Task<Result<ChiTietHoaDonNoDto>> CreateAsync(ChiTietHoaDonNoDto dto);
    Task<Result<ChiTietHoaDonNoDto>> UpdateAsync(Guid id, ChiTietHoaDonNoDto dto);
    Task<Result<ChiTietHoaDonNoDto>> DeleteAsync(Guid id);
    Task<Result<ChiTietHoaDonNoDto>> RestoreAsync(Guid id);
}
