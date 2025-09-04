using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class SanPhamDataProvider : BaseDataProvider<SanPhamDto>
    {
        public SanPhamDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
