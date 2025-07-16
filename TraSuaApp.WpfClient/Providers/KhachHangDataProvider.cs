using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class KhachHangDataProvider : BaseDataProvider<KhachHangDto>
    {
        public KhachHangDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}