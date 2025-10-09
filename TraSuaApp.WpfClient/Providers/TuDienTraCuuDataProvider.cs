using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class TuDienTraCuuDataProvider : BaseDataProvider<TuDienTraCuuDto>
    {
        public TuDienTraCuuDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
