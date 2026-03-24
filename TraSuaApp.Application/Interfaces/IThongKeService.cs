using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Applicationn.Interfaces
{
    public interface IThongKeService
    {
        Task<ThongKeChiTieuDto> TinhChiTieuNgayAsync(DateTime ngay);

        Task<ThongKeCongNoDto> TinhCongNoNgayAsync(DateTime ngay);

        Task<ThongKeThanhToanDto> TinhThanhToanNgayAsync(DateTime ngay);

        Task<ThongKeDoanhThuNgayDto> TinhDoanhThuNgayAsync(DateTime ngay);

        Task<ThongKeTraNoNgayDto> TinhTraNoNgayAsync(DateTime ngay);

        Task<ThongKeDonChuaThanhToanDto> TinhDonChuaThanhToanAsync(DateTime ngay);
    }
}

