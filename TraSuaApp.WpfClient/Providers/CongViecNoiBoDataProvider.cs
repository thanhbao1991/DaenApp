using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Providers;

namespace TraSuaApp.WpfClient.DataProviders
{
    public class CongViecNoiBoDataProvider : BaseDataProvider<CongViecNoiBoDto>
    {
        public CongViecNoiBoDataProvider(ISignalRClient signalR) : base(signalR)
        {
            // 🟟 Khi BaseDataProvider bắn OnChanged → bắn ra event ItemsChanged cho UI
            base.OnChanged += () => ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        // 🟟 Public event cho UI hoặc Dashboard hook vào
        public event EventHandler? ItemsChanged;
    }
}