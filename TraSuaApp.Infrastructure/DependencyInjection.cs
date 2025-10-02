using Microsoft.Extensions.DependencyInjection;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Infrastructure.Services;

namespace TraSuaApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Đăng ký tất cả các service tại đây
            services.AddScoped<IDoanhThuService, DoanhThuService>();
            services.AddScoped<IThongKeService, ThongKeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaiKhoanService, TaiKhoanService>();
            services.AddScoped<INhomSanPhamService, NhomSanPhamService>();
            services.AddScoped<ISanPhamService, SanPhamService>();
            services.AddScoped<IToppingService, ToppingService>();
            services.AddScoped<IKhachHangService, KhachHangService>();
            services.AddScoped<IVoucherService, VoucherService>();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IHoaDonService, HoaDonService>();
            services.AddScoped<IPhuongThucThanhToanService, PhuongThucThanhToanService>();
            services.AddScoped<ICongViecNoiBoService, CongViecNoiBoService>();
            services.AddScoped<IChiTietHoaDonNoService, ChiTietHoaDonNoService>();
            services.AddScoped<IChiTietHoaDonThanhToanService, ChiTietHoaDonThanhToanService>();
            services.AddScoped<INguyenLieuService, NguyenLieuService>();
            services.AddScoped<IChiTieuHangNgayService, ChiTieuHangNgayService>();
            services.AddScoped<IKhachHangGiaBanService, KhachHangGiaBanService>();



            return services;
        }
    }
}
