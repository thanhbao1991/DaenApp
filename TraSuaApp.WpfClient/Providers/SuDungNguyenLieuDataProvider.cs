using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class SuDungNguyenLieuDataProvider : BaseDataProvider<SuDungNguyenLieuDto>
    {
        public SuDungNguyenLieuDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
