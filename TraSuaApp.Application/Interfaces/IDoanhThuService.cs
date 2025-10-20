using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IDoanhThuService
{
    Task<DoanhThuNgayDto> GetDoanhThuNgayAsync(DateTime ngay);
    Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam);
    Task<List<DoanhThuChiTietHoaDonDto>> GetChiTietHoaDonAsync(Guid hoaDonId);

    // 🟟 mới: danh sách hóa đơn theo khách trong ngày
    Task<List<DoanhThuHoaDonDto>> GetHoaDonKhachHangAsync(Guid khachHangId);

    // 🟟 mới: tổng số đơn theo giờ trong THÁNG (gộp 1 query)
    Task<List<DoanhThuHourBucketDto>> GetSoDonTheoGioTrongThangAsync(int thang, int nam, int startHour = 6, int endHour = 22);
}