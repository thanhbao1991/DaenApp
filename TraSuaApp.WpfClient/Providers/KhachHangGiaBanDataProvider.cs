using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class KhachHangGiaBanDataProvider : BaseDataProvider<KhachHangGiaBanDto>
    {
        public KhachHangGiaBanDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
