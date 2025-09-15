using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface IDoanhThuService
{
    Task<DoanhThuNgayDto> GetDoanhThuNgayAsync(DateTime ngay);
    Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam);
    Task<List<DoanhThuChiTietHoaDonDto>> GetChiTietHoaDonAsync(Guid hoaDonId); // 🟟 thêm
}