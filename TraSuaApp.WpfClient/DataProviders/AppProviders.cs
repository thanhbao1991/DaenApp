using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;

public static class AppProviders
{
    public static string? CurrentConnectionId { get; set; }

    // Các provider không khởi tạo ngay, sẽ gán trong InitializeAsync
    public static VoucherDataProvider? Vouchers { get; private set; }
    public static SanPhamDataProvider? SanPhams { get; private set; }
    public static TaiKhoanDataProvider? TaiKhoans { get; private set; }
    public static ToppingDataProvider? Toppings { get; private set; }
    public static KhachHangDataProvider? KhachHangs { get; private set; }
    public static NhomSanPhamDataProvider? NhomSanPhams { get; private set; }
    public static HoaDonDataProvider? HoaDons { get; private set; }
    public static PhuongThucThanhToanDataProvider? PhuongThucThanhToans { get; private set; }

    public static async Task InitializeAsync()
    {
        var baseUri = ApiClient.BaseAddress!.ToString().TrimEnd('/');
        var signalR = new SignalRClient($"{baseUri}/hub/entity");

        await signalR.ConnectAsync();
        CurrentConnectionId = await signalR.GetConnectionId();
        ApiClient.ConnectionId = CurrentConnectionId;

        KhachHangs = new KhachHangDataProvider(signalR);
        NhomSanPhams = new NhomSanPhamDataProvider(signalR);
        Toppings = new ToppingDataProvider(signalR);
        TaiKhoans = new TaiKhoanDataProvider(signalR);
        SanPhams = new SanPhamDataProvider(signalR);
        Vouchers = new VoucherDataProvider(signalR);
        HoaDons = new HoaDonDataProvider(signalR);
        PhuongThucThanhToans = new PhuongThucThanhToanDataProvider(signalR);

        // Load tuần tự hoặc song song tuỳ bạn
        await Task.WhenAll(
           KhachHangs.InitializeAsync(),
           NhomSanPhams.InitializeAsync(),
           Toppings.InitializeAsync(),
           TaiKhoans.InitializeAsync(),
           SanPhams.InitializeAsync(),
           Vouchers.InitializeAsync(),
           HoaDons.InitializeAsync(),
           PhuongThucThanhToans.InitializeAsync()
        );
    }
}
