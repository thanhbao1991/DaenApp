using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class NguyenLieuBanHangDataProvider : BaseDataProvider<NguyenLieuBanHangDto>
    {
        public NguyenLieuBanHangDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
