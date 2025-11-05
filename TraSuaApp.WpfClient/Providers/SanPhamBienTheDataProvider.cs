using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class SanPhamBienTheDataProvider : BaseDataProvider<SanPhamBienTheDto>
    {
        public SanPhamBienTheDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
