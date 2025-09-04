using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class NguyenLieuDataProvider : BaseDataProvider<NguyenLieuDto>
    {
        public NguyenLieuDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
