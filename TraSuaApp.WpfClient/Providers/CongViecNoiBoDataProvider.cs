using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;
namespace TraSuaApp.WpfClient.DataProviders
{
    public class CongViecNoiBoDataProvider : BaseDataProvider<CongViecNoiBoDto>
    {
        public CongViecNoiBoDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
