using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class NguyenLieuTransactionDataProvider : BaseDataProvider<NguyenLieuTransactionDto>
    {
        public NguyenLieuTransactionDataProvider(ISignalRClient signalR) : base(signalR) { }
    }
}
