using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces
{
    public interface IThongKeService
    {
        Task<ThongKeNgayDto> TinhNgayAsync(DateTime ngay);

    }
}