using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class ChiTietHoaDonThanhToanDataProvider : BaseDataProvider<ChiTietHoaDonThanhToanDto>
    {
        public ChiTietHoaDonThanhToanDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
