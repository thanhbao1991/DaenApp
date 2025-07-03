using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.TenDangNhap == request.TaiKhoan);

        if (user == null)
        {
            return new LoginResponse
            {
                ThanhCong = false,
                Message = "Tài khoản không tồn tại"
            };
        }

        if (!user.IsActive)
        {
            return new LoginResponse
            {
                ThanhCong = false,
                Message = "Tài khoản đã bị khoá"
            };
        }

        // ✅ Dùng BCrypt thay vì so sánh chuỗi thuần
        if (!PasswordHelper.VerifyPassword(request.MatKhau, user.MatKhau))
        {
            return new LoginResponse
            {
                ThanhCong = false,
                Message = "Mật khẩu không đúng"
            };
        }

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponse
        {
            ThanhCong = true,
            Message = "Đăng nhập thành công",
            TenHienThi = user.TenHienThi,
            VaiTro = user.VaiTro,
            Token = token
        };
    }
}