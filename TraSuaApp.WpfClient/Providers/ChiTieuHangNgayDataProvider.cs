using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class ChiTieuHangNgayDataProvider : BaseDataProvider<ChiTieuHangNgayDto>
    {
        public ChiTieuHangNgayDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
