using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class VoucherDataProvider : BaseDataProvider<VoucherDto>
    {
        public VoucherDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
