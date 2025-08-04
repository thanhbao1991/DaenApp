using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class HoaDonDataProvider : BaseDataProvider<HoaDonDto>
    {
        public HoaDonDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
