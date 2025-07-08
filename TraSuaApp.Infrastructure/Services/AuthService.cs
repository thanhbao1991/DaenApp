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

    public async Task<Result> LoginAsync(LoginRequest request)
    {
        var user = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.TenDangNhap == request.TaiKhoan);

        if (user == null)
        {
            return Result.Failure("Tài khoản không tồn tại");
        }

        if (!user.IsActive)
        {
            return Result.Failure("Tài khoản đã bị khoá");
        }

        if (!PasswordHelper.VerifyPassword(request.MatKhau, user.MatKhau))
        {
            return Result.Failure("Mật khẩu không đúng");
        }

        var token = _jwtTokenService.GenerateToken(user);

        var response = new LoginResponse
        {
            ThanhCong = true,
            Message = "Đăng nhập thành công",
            TenHienThi = user.TenHienThi,
            VaiTro = user.VaiTro,
            Token = token
        };

        return Result.Success("Đăng nhập thành công")
            .WithId(user.Id)
            .WithAfter(response);
    }
}