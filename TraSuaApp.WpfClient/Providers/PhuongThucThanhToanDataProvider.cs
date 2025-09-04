using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class PhuongThucThanhToanDataProvider : BaseDataProvider<PhuongThucThanhToanDto>
    {
        public PhuongThucThanhToanDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
