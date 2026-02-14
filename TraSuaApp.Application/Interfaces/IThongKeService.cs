using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Applicationn.Interfaces
{
    public interface IThongKeService
    {
        Task<ThongKeNgayDto> TinhNgayAsync(DateTime ngay);
        Task<ThongKeNgayDto> TinhNgay_AnShipKhanhAsync(DateTime ngay);
    }
}