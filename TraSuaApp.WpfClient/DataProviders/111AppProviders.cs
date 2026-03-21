using System.Media;
using System.Net.Http.Json;
using System.Windows;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;

public static class AppProviders
{
    public static string? CurrentConnectionId { get; private set; }

    public static NguyenLieuTransactionDataProvider? NguyenLieuTransactions { get; private set; }
    public static LocationDataProvider? Locations { get; private set; }
    public static TuDienTraCuuDataProvider? TuDienTraCuus { get; private set; }
    public static NguyenLieuBanHangDataProvider? NguyenLieuBanHangs { get; private set; }
    public static CongThucDataProvider? CongThucs { get; private set; }
    public static SuDungNguyenLieuDataProvider? SuDungNguyenLieus { get; private set; }
    public static VoucherDataProvider? Vouchers { get; private set; }
    public static SanPhamDataProvider? SanPhams { get; private set; }
    public static SanPhamDataProvider? SanPhamBienThes { get; private set; }
    public static TaiKhoanDataProvider? TaiKhoans { get; private set; }
    public static ToppingDataProvider? Toppings { get; private set; }
    public static KhachHangDataProvider? KhachHangs { get; private set; }
    public static NhomSanPhamDataProvider? NhomSanPhams { get; private set; }
    public static HoaDonDataProvider? HoaDons { get; private set; }
    public static PhuongThucThanhToanDataProvider? PhuongThucThanhToans { get; private set; }
    public static KhachHangGiaBanDataProvider? KhachHangGiaBans { get; private set; }
    public static CongViecNoiBoDataProvider? CongViecNoiBos { get; private set; }
    public static ChiTietHoaDonThanhToanDataProvider? ChiTietHoaDonThanhToans { get; private set; }
    public static ChiTieuHangNgayDataProvider? ChiTieuHangNgays { get; private set; }
    public static NguyenLieuDataProvider? NguyenLieus { get; private set; }

    public static string QuickOrderMenu { get; private set; } = "";

    private static SignalRClient? _signalR;
    private static bool _created;

    // 🟟 danh sách provider để dispatch signal
    private static List<object> _providers = new();

    public static void BuildQuickOrderMenu()
    {
        var items = SanPhams?.Items?
            .Where(x => !x.NgungBan)
            .OrderBy(x => x.Ten)
            .Select(x => $"{x.Id}\t{x.TenKhongVietTat}") ?? Enumerable.Empty<string>();

        QuickOrderMenu = string.Join("\n", items);
    }

    public static async Task ReloadAllAsync()
    {
        if (HoaDons != null) await HoaDons.ReloadAsync();
        if (ChiTietHoaDonThanhToans != null) await ChiTietHoaDonThanhToans.ReloadAsync();
        if (ChiTieuHangNgays != null) await ChiTieuHangNgays.ReloadAsync();
        if (CongViecNoiBos != null) await CongViecNoiBos.ReloadAsync();
        if (Toppings != null) await Toppings.ReloadAsync();
        if (KhachHangs != null) await KhachHangs.ReloadAsync();
        if (Vouchers != null) await Vouchers.ReloadAsync();
        if (KhachHangGiaBans != null) await KhachHangGiaBans.ReloadAsync();
        if (PhuongThucThanhToans != null) await PhuongThucThanhToans.ReloadAsync();
        if (TuDienTraCuus != null) await TuDienTraCuus.ReloadAsync();
        if (NguyenLieuBanHangs != null) await NguyenLieuBanHangs.ReloadAsync();
        if (CongThucs != null) await CongThucs.ReloadAsync();
        if (SuDungNguyenLieus != null) await SuDungNguyenLieus.ReloadAsync();
        if (Locations != null) await Locations.ReloadAsync();
        if (SanPhamBienThes != null) await SanPhamBienThes.ReloadAsync();

        if (SanPhams != null)
        {
            await SanPhams.ReloadAsync();
            BuildQuickOrderMenu();
        }
    }

    public static async Task<DashboardDto?> GetDashboardAsync()
    {
        var response = await ApiClient.GetAsync("/api/dashboard/homnay");
        return await response.Content.ReadFromJsonAsync<DashboardDto>();
    }

    public static async Task EnsureCreatedAsync()
    {
        if (_created) return;

        var baseUri = Config.SignalRHubUrl.TrimEnd('/');
        _signalR = new SignalRClient($"{baseUri}");

        try
        {
            await _signalR.ConnectAsync();
        }
        catch { }

        CurrentConnectionId = await _signalR.GetConnectionId();
        ApiClient.ConnectionId = CurrentConnectionId;

        // ================================
        // 🟟 CONNECTION EVENTS
        // ================================
        _signalR.OnDisconnected(() =>
        {
            SystemSounds.Hand.Play();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();

                if (existing == null)
                    new TraSuaApp.WpfClient.Views.NotiWindow().Show();
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();

                existing?.Close();
            });
        });

        // ================================
        // 🟟 INIT PROVIDERS
        // ================================
        KhachHangs = new KhachHangDataProvider(_signalR);
        NhomSanPhams = new NhomSanPhamDataProvider(_signalR);
        Toppings = new ToppingDataProvider(_signalR);
        TaiKhoans = new TaiKhoanDataProvider(_signalR);
        SanPhams = new SanPhamDataProvider(_signalR);
        SanPhamBienThes = new SanPhamDataProvider(_signalR);
        SanPhams.OnChanged += BuildQuickOrderMenu;

        Vouchers = new VoucherDataProvider(_signalR);
        HoaDons = new HoaDonDataProvider(_signalR);
        PhuongThucThanhToans = new PhuongThucThanhToanDataProvider(_signalR);
        TuDienTraCuus = new TuDienTraCuuDataProvider(_signalR);
        NguyenLieuBanHangs = new NguyenLieuBanHangDataProvider(_signalR);
        CongThucs = new CongThucDataProvider(_signalR);
        SuDungNguyenLieus = new SuDungNguyenLieuDataProvider(_signalR);
        Locations = new LocationDataProvider(_signalR);
        KhachHangGiaBans = new KhachHangGiaBanDataProvider(_signalR);
        CongViecNoiBos = new CongViecNoiBoDataProvider(_signalR);
        ChiTietHoaDonThanhToans = new ChiTietHoaDonThanhToanDataProvider(_signalR);
        NguyenLieus = new NguyenLieuDataProvider(_signalR);
        ChiTieuHangNgays = new ChiTieuHangNgayDataProvider(_signalR);

        // 🟟 REGISTER LIST
        _providers = new List<object>
        {
            KhachHangs, NhomSanPhams, Toppings, TaiKhoans,
            SanPhams, SanPhamBienThes, Vouchers, HoaDons,
            PhuongThucThanhToans, TuDienTraCuus, NguyenLieuBanHangs,
            CongThucs, SuDungNguyenLieus, Locations,
            KhachHangGiaBans, CongViecNoiBos,
            ChiTietHoaDonThanhToans, NguyenLieus, ChiTieuHangNgays
        };

        // ================================
        // 🟟 SIGNAL DISPATCH (CHUẨN)
        // ================================
        _signalR.Subscribe("EntityChanged", async (string entityName, string action, string id, string senderConnectionId) =>
        {
            // 🟟 bỏ qua chính mình
            if (!string.IsNullOrWhiteSpace(senderConnectionId) &&
                senderConnectionId == CurrentConnectionId)
                return;

            foreach (var provider in _providers)
            {
                if (provider == null) continue;

                var type = provider.GetType();
                var entityProp = type.GetProperty("EntityName");
                var handleMethod = type.GetMethod("HandleSignalAsync");

                if (entityProp == null || handleMethod == null) continue;

                var entity = entityProp.GetValue(provider)?.ToString();

                if (string.Equals(entity, entityName, StringComparison.OrdinalIgnoreCase))
                {
                    await (Task)handleMethod.Invoke(provider, new object[] { action, id });
                    break;
                }
            }
            // ================================
            // 🟟 PUSH RIÊNG CHO UI HÓA ĐƠN
            // ================================
            if (entityName.Equals("HoaDon", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(id, out var guid))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var dashboard = Application.Current.Windows
                            .OfType<TraSuaApp.WpfClient.Views.Dashboard>()
                            .FirstOrDefault();

                        dashboard?.HoaDonTabControl?.HandleHoaDonSignal(guid);
                    });
                }
            }
            // ================================
            // 🟟 TTS (giữ nguyên)
            // ================================
            if (entityName.Equals("HoaDon", StringComparison.OrdinalIgnoreCase))
            {
                var hoaDon = HoaDons?.Items.FirstOrDefault(x => x.Id.ToString() == id);

                if (hoaDon != null && action == "updatedEsc")
                {
                    TTSHelper.DownloadAndPlayGoogleTTSAsync(
                        $"Đi ship {hoaDon.DiaChiText} {((long)hoaDon.ThanhTien)} đồng"
                    );
                }
            }
        });

        _created = true;
    }

    public static async Task InitializeAsync()
    {
        await EnsureCreatedAsync();

        await Task.WhenAll(
           KhachHangs!.InitializeAsync(),
           NhomSanPhams!.InitializeAsync(),
           Toppings!.InitializeAsync(),
           TaiKhoans!.InitializeAsync(),
           SanPhams!.InitializeAsync(),
           SanPhamBienThes!.InitializeAsync(),
           Vouchers!.InitializeAsync(),
           HoaDons!.InitializeAsync(),
           PhuongThucThanhToans!.InitializeAsync(),
           TuDienTraCuus!.InitializeAsync(),
           NguyenLieuBanHangs!.InitializeAsync(),
           CongThucs!.InitializeAsync(),
           SuDungNguyenLieus!.InitializeAsync(),
           Locations!.InitializeAsync(),
           KhachHangGiaBans!.InitializeAsync(),
           CongViecNoiBos!.InitializeAsync(),
           ChiTietHoaDonThanhToans!.InitializeAsync(),
           NguyenLieus!.InitializeAsync(),
           ChiTieuHangNgays!.InitializeAsync()
        );

        BuildQuickOrderMenu();
    }
}