using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IDoanhThuService
{
    Task<DoanhThuNgayDto> GetDoanhThuNgayAsync(DateTime ngay);
    Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam);
    Task<List<DoanhThuChiTietHoaDonDto>> GetChiTietHoaDonAsync(Guid hoaDonId);

    Task<List<DoanhThuHoaDonDto>> GetHoaDonKhachHangAsync(Guid khachHangId);

    // Giữ method cũ, nhưng sẽ trả về cả SoDon và DoanhThu trong DTO
    Task<List<DoanhThuHourBucketDto>> GetSoDonTheoGioTrongThangAsync(int thang, int nam, int startHour = 6, int endHour = 22);
}