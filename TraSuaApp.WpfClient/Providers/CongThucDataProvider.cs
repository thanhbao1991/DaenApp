using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class CongThucDataProvider : BaseDataProvider<CongThucDto>
    {
        public CongThucDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
