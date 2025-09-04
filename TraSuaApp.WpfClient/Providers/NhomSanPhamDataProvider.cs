using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class NhomSanPhamDataProvider : BaseDataProvider<NhomSanPhamDto>
    {
        public NhomSanPhamDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
