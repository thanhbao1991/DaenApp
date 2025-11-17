using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class LocationDataProvider : BaseDataProvider<LocationDto>
    {
        public LocationDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
