using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IHoaDonService
{
    Task<List<HoaDonDto>> GetAllAsync();
    Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonDto>> DeleteAsync(Guid id);
    Task<Result<HoaDonDto>> RestoreAsync(Guid id);
    Task<List<HoaDonDto>> GetUpdatedSince(DateTime lastSync);
    Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto);














    Task<HoaDonDto?> GetByIdAsync(Guid id);
    Task<KhachHangInfoDto?> GetKhachHangInfoAsync(Guid khachHangId);
    Task<Result<HoaDonNoDto>> UpdateEscSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateRollBackSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdatePrintSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateF12SingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateF1F4SingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto);
}
