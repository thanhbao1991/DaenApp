using Microsoft.Extensions.DependencyInjection;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Infrastructure.Services;

namespace TraSuaApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Đăng ký tất cả các service tại đây
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITaiKhoanService, TaiKhoanService>();
            services.AddScoped<INhomSanPhamService, NhomSanPhamService>();
            services.AddScoped<ISanPhamService, SanPhamService>();
            services.AddScoped<IToppingService, ToppingService>();

            // Nếu có các service khác, thêm tiếp ở đây

            return services;
        }
    }
}