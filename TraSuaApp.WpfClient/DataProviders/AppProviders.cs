using System.Net.Http.Json;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;

public static class AppProviders
{
    public static string? CurrentConnectionId { get; set; }

    public static VoucherDataProvider Vouchers { get; private set; } = null!;
    public static SanPhamDataProvider SanPhams { get; private set; } = null!;
    public static TaiKhoanDataProvider TaiKhoans { get; private set; } = null!;
    public static ToppingDataProvider Toppings { get; private set; } = null!;
    public static KhachHangDataProvider KhachHangs { get; private set; } = null!;
    public static NhomSanPhamDataProvider NhomSanPhams { get; private set; } = null!;
    public static HoaDonDataProvider HoaDons { get; private set; } = null!;

    public static PhuongThucThanhToanDataProvider PhuongThucThanhToans { get; private set; } = null!;
    public static CongViecNoiBoDataProvider CongViecNoiBos { get; private set; } = null!;
    public static ChiTietHoaDonNoDataProvider ChiTietHoaDonNos { get; private set; } = null!;
    public static ChiTietHoaDonThanhToanDataProvider ChiTietHoaDonThanhToans { get; private set; } = null!;
    public static ChiTieuHangNgayDataProvider ChiTieuHangNgays { get; private set; } = null!;
    public static NguyenLieuDataProvider NguyenLieus { get; private set; } = null!;

    public static async Task ReloadAllAsync()
    {
        if (HoaDons != null) await HoaDons.ReloadAsync();
        if (ChiTietHoaDonThanhToans != null) await ChiTietHoaDonThanhToans.ReloadAsync();
        if (ChiTietHoaDonNos != null) await ChiTietHoaDonNos.ReloadAsync();
        if (ChiTieuHangNgays != null) await ChiTieuHangNgays.ReloadAsync();
        if (CongViecNoiBos != null) await CongViecNoiBos.ReloadAsync();
        if (SanPhams != null) await SanPhams.ReloadAsync();
        if (Toppings != null) await Toppings.ReloadAsync();
        if (KhachHangs != null) await KhachHangs.ReloadAsync();
        if (Vouchers != null) await Vouchers.ReloadAsync();
    }

    public static async Task<DashboardDto?> GetDashboardAsync()
    {
        var response = await ApiClient.GetAsync("/api/dashboard/homnay");
        return await response.Content.ReadFromJsonAsync<DashboardDto>();
    }

    public static async Task InitializeAsync()
    {
        var baseUri = Config.SignalRHubUrl.TrimEnd('/');

        var signalR = new SignalRClient($"{baseUri}");

        try
        {
            await signalR.ConnectAsync();
            //NotiHelper.Show("✅ Kết nối Sever Đồng Bộ thành công");
        }
        catch (Exception ex)
        {
            NotiHelper.ShowError($"❌ Không thể kết nối Sever Đồng Bộ: {ex.Message}");
        }

        CurrentConnectionId = await signalR.GetConnectionId();
        ApiClient.ConnectionId = CurrentConnectionId;

        // 🟟 handle mất kết nối / kết nối lại
        signalR.OnDisconnected(() =>
        {
            NotiHelper.ShowError("⚠️ Mất kết nối Sever Đồng Bộ. Dữ liệu sẽ reload định kỳ 5 phút.");
        });

        signalR.OnReconnected(() =>
        {
            NotiHelper.Show("✅ Đã kết nối lại Sever Đồng Bộ.");
        });

        KhachHangs = new KhachHangDataProvider(signalR);
        NhomSanPhams = new NhomSanPhamDataProvider(signalR);
        Toppings = new ToppingDataProvider(signalR);
        TaiKhoans = new TaiKhoanDataProvider(signalR);
        SanPhams = new SanPhamDataProvider(signalR);
        Vouchers = new VoucherDataProvider(signalR);
        HoaDons = new HoaDonDataProvider(signalR);
        PhuongThucThanhToans = new PhuongThucThanhToanDataProvider(signalR);
        CongViecNoiBos = new CongViecNoiBoDataProvider(signalR);
        ChiTietHoaDonNos = new ChiTietHoaDonNoDataProvider(signalR);
        ChiTietHoaDonThanhToans = new ChiTietHoaDonThanhToanDataProvider(signalR);
        NguyenLieus = new NguyenLieuDataProvider(signalR);
        ChiTieuHangNgays = new ChiTieuHangNgayDataProvider(signalR);

        // 🟟 load ban đầu
        await Task.WhenAll(
           KhachHangs.InitializeAsync(),
           NhomSanPhams.InitializeAsync(),
           Toppings.InitializeAsync(),
           TaiKhoans.InitializeAsync(),
           SanPhams.InitializeAsync(),
           Vouchers.InitializeAsync(),
           HoaDons.InitializeAsync(),
           PhuongThucThanhToans.InitializeAsync(),
           CongViecNoiBos.InitializeAsync(),
           ChiTietHoaDonNos.InitializeAsync(),
           ChiTietHoaDonThanhToans.InitializeAsync(),
           NguyenLieus.InitializeAsync(),
           ChiTieuHangNgays.InitializeAsync()
        );
    }
}