using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class ToppingDataProvider : BaseDataProvider<ToppingDto>
    {
        public ToppingDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}