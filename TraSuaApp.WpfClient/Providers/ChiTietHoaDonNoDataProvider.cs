using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class ChiTietHoaDonNoDataProvider : BaseDataProvider<ChiTietHoaDonNoDto>
    {
        public ChiTietHoaDonNoDataProvider(ISignalRClient signalR) : base(signalR) { }

    }

}
