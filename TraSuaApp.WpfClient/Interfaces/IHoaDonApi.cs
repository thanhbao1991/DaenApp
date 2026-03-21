using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IHoaDonApi
{
    Task<Result<List<HoaDonDto>>> GetAllAsync();
    Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since);

    Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto);

    Task<Result<HoaDonDto>> DeleteAsync(Guid id);
    Task<Result<HoaDonDto>> RestoreAsync(Guid id);

    Task<Result<HoaDonDto>> GetByIdAsync(Guid id);
    Task<Result<KhachHangInfoDto>> GetKhachHangInfoAsync(Guid khachHangId);

    // ===== Các update nhanh cho list (trả về HoaDonNoDto) =====
    Task<Result<HoaDonNoDto>> UpdateEscSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateRollBackSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdatePrintSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateF12SingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonNoDto>> UpdateF1F4SingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto);
}