using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Services;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.TenDangNhap == request.TaiKhoan);

        if (user == null)
            return Result<LoginResponse>.Failure("Tài khoản không tồn tại");

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("Tài khoản đã bị khoá");

        if (!PasswordHelper.VerifyPassword(request.MatKhau, user.MatKhau))
            return Result<LoginResponse>.Failure("Mật khẩu không đúng");

        var token = _jwtTokenService.GenerateToken(user);

        var response = new LoginResponse
        {
            ThanhCong = true,
            Message = "Đăng nhập thành công",
            TenHienThi = user.TenHienThi,
            VaiTro = user.VaiTro,
            Token = token
        };

        return Result<LoginResponse>.Success("Đăng nhập thành công", response)
            .WithId(user.Id)
            .WithAfter(response);
    }
}