using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient
{

    public static class AppProviders
    {
        public static string? CurrentConnectionId { get; set; }
        public static async Task InitializeAsync()
        {
            var baseUri = ApiClient.BaseAddress!.ToString().TrimEnd('/');
            var signalR = new SignalRClient($"{baseUri}/hub/entity");

            await signalR.ConnectAsync(); // ép kết nối trước

            AppProviders.CurrentConnectionId = await signalR.GetConnectionId(); // đảm bảo luôn có
            ApiClient.ConnectionId = AppProviders.CurrentConnectionId;

            KhachHangs = new KhachHangDataProvider(signalR);
            await KhachHangs.InitializeAsync();

            NhomSanPhams = new NhomSanPhamDataProvider(signalR);
            await NhomSanPhams.InitializeAsync();

            Toppings = new ToppingDataProvider(signalR);
            await Toppings.InitializeAsync();


            TaiKhoans = new TaiKhoanDataProvider(signalR);
            await TaiKhoans.InitializeAsync();
        }
        public static TaiKhoanDataProvider TaiKhoans { get; private set; } = null!;

        public static ToppingDataProvider Toppings { get; private set; } = null!;
        public static KhachHangDataProvider KhachHangs { get; private set; } = null!;
        public static NhomSanPhamDataProvider NhomSanPhams { get; private set; } = null!;
    }
}