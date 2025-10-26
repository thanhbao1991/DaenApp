using System.Media;
using System.Net.Http.Json;
using System.Windows;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;

public static class AppProviders
{
    public static string? CurrentConnectionId { get; private set; }

    // Providers
    public static TuDienTraCuuDataProvider? TuDienTraCuus { get; private set; }
    public static VoucherDataProvider? Vouchers { get; private set; }
    public static SanPhamDataProvider? SanPhams { get; private set; }
    public static TaiKhoanDataProvider? TaiKhoans { get; private set; }
    public static ToppingDataProvider? Toppings { get; private set; }
    public static KhachHangDataProvider? KhachHangs { get; private set; }
    public static NhomSanPhamDataProvider? NhomSanPhams { get; private set; }
    public static HoaDonDataProvider? HoaDons { get; private set; }
    public static PhuongThucThanhToanDataProvider? PhuongThucThanhToans { get; private set; }
    public static KhachHangGiaBanDataProvider? KhachHangGiaBans { get; private set; }
    public static CongViecNoiBoDataProvider? CongViecNoiBos { get; private set; }
    public static ChiTietHoaDonNoDataProvider? ChiTietHoaDonNos { get; private set; }
    public static ChiTietHoaDonThanhToanDataProvider? ChiTietHoaDonThanhToans { get; private set; }
    public static ChiTieuHangNgayDataProvider? ChiTieuHangNgays { get; private set; }
    public static NguyenLieuDataProvider? NguyenLieus { get; private set; }

    public static string QuickOrderMenu { get; private set; } = "";

    private static SignalRClient? _signalR;
    private static bool _created;

    // =======================
    //  QUICK ORDER HELPER
    // =======================
    public static void BuildQuickOrderMenu()
    {
        var items = SanPhams?.Items?
            .Where(x => !x.NgungBan)
            .OrderBy(x => x.Ten)
            .Select(x => $"{x.Id}\t{x.TenKhongVietTat}") ?? Enumerable.Empty<string>();

        QuickOrderMenu = string.Join("\n", items);
    }

    // =======================
    //  ON-DEMAND RELOAD ALL (nếu cần)
    // =======================
    public static async Task ReloadAllAsync()
    {
        if (HoaDons != null) await HoaDons.ReloadAsync();
        if (ChiTietHoaDonThanhToans != null) await ChiTietHoaDonThanhToans.ReloadAsync();
        if (ChiTietHoaDonNos != null) await ChiTietHoaDonNos.ReloadAsync();
        if (ChiTieuHangNgays != null) await ChiTieuHangNgays.ReloadAsync();
        if (CongViecNoiBos != null) await CongViecNoiBos.ReloadAsync();
        if (Toppings != null) await Toppings.ReloadAsync();
        if (KhachHangs != null) await KhachHangs.ReloadAsync();
        if (Vouchers != null) await Vouchers.ReloadAsync();
        if (KhachHangGiaBans != null) await KhachHangGiaBans.ReloadAsync();
        if (PhuongThucThanhToans != null) await PhuongThucThanhToans.ReloadAsync();
        if (TuDienTraCuus != null) await TuDienTraCuus.ReloadAsync();

        if (SanPhams != null)
        {
            await SanPhams.ReloadAsync();
            BuildQuickOrderMenu();
        }
    }

    // =======================
    //  DASHBOARD SUMMARY
    // =======================
    public static async Task<DashboardDto?> GetDashboardAsync()
    {
        var response = await ApiClient.GetAsync("/api/dashboard/homnay");
        return await response.Content.ReadFromJsonAsync<DashboardDto>();
    }

    // =====================================================
    //  NEW: KHỞI TẠO TỐI THIỂU (KHÔNG LOAD DỮ LIỆU)
    //  - Kết nối SignalR
    //  - Tạo instance providers
    //  - Gắn sự kiện connect/disconnect
    // =====================================================
    public static async Task EnsureCreatedAsync()
    {
        if (_created) return;

        var baseUri = Config.SignalRHubUrl.TrimEnd('/');
        _signalR = new SignalRClient($"{baseUri}");

        try
        {
            await _signalR.ConnectAsync(); // nhẹ, không gọi API data
        }
        catch
        {
            // im lặng – UI có thể hiển thị toast riêng nếu muốn
        }

        CurrentConnectionId = await _signalR.GetConnectionId();
        ApiClient.ConnectionId = CurrentConnectionId;

        _signalR.OnDisconnected(() =>
        {
            SystemSounds.Hand.Play();

            App.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();

                if (existing == null)
                {
                    var win = new TraSuaApp.WpfClient.Views.NotiWindow();
                    win.Show();
                }
                else
                {
                    existing.Topmost = true;
                    existing.Focus();
                }
            });
        });

        _signalR.OnReconnected(() =>
        {
            SystemSounds.Asterisk.Play();

            App.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();
                existing?.Close();
            });
        });

        // Chỉ TẠO INSTANCE provider (KHÔNG gọi InitializeAsync hay Reload)
        KhachHangs = new KhachHangDataProvider(_signalR);
        NhomSanPhams = new NhomSanPhamDataProvider(_signalR);
        Toppings = new ToppingDataProvider(_signalR);
        TaiKhoans = new TaiKhoanDataProvider(_signalR);
        SanPhams = new SanPhamDataProvider(_signalR);
        SanPhams.OnChanged += BuildQuickOrderMenu;

        Vouchers = new VoucherDataProvider(_signalR);
        HoaDons = new HoaDonDataProvider(_signalR);
        PhuongThucThanhToans = new PhuongThucThanhToanDataProvider(_signalR);
        TuDienTraCuus = new TuDienTraCuuDataProvider(_signalR);
        KhachHangGiaBans = new KhachHangGiaBanDataProvider(_signalR);
        CongViecNoiBos = new CongViecNoiBoDataProvider(_signalR);
        ChiTietHoaDonNos = new ChiTietHoaDonNoDataProvider(_signalR);
        ChiTietHoaDonThanhToans = new ChiTietHoaDonThanhToanDataProvider(_signalR);
        NguyenLieus = new NguyenLieuDataProvider(_signalR);
        ChiTieuHangNgays = new ChiTieuHangNgayDataProvider(_signalR);

        _created = true;
    }

    // =====================================================================
    //  FULL INIT (để tương thích code cũ): tạo + tải dữ liệu ban đầu (nếu cần)
    //  -> Login chỉ token: GỌI EnsureCreatedAsync() thôi.
    //  -> Khi cần prefetch: gọi InitializeAsync() như cũ.
    // =====================================================================
    public static async Task InitializeAsync()
    {
        await EnsureCreatedAsync(); // đảm bảo đã có signalR + providers

        // 🟟 load ban đầu (có thể gọi sau này theo on-demand)
        await Task.WhenAll(
           KhachHangs!.InitializeAsync(),
           NhomSanPhams!.InitializeAsync(),
           Toppings!.InitializeAsync(),
           TaiKhoans!.InitializeAsync(),
           SanPhams!.InitializeAsync(),
           Vouchers!.InitializeAsync(),
           HoaDons!.InitializeAsync(),
           PhuongThucThanhToans!.InitializeAsync(),
           TuDienTraCuus!.InitializeAsync(),
           KhachHangGiaBans!.InitializeAsync(),
           CongViecNoiBos!.InitializeAsync(),
           ChiTietHoaDonNos!.InitializeAsync(),
           ChiTietHoaDonThanhToans!.InitializeAsync(),
           NguyenLieus!.InitializeAsync(),
           ChiTieuHangNgays!.InitializeAsync()
        );

        BuildQuickOrderMenu();
    }
}