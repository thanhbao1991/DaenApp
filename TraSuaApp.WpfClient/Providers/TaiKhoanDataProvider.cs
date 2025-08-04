using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class TaiKhoanDataProvider : BaseDataProvider<TaiKhoanDto>
    {
        public TaiKhoanDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
