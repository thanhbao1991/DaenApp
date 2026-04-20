using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.TenDangNhap == request.TaiKhoan);

        if (user == null)
            return Unauthorized(Result<LoginResponse>.Failure("Tài khoản không tồn tại"));

        if (!user.IsActive)
            return Unauthorized(Result<LoginResponse>.Failure("Tài khoản đã bị khoá"));

        if (!PasswordHelper.VerifyPassword(request.MatKhau, user.MatKhau))
            return Unauthorized(Result<LoginResponse>.Failure("Mật khẩu không đúng"));

        var token = GenerateToken(user);

        var response = new LoginResponse
        {
            ThanhCong = true,
            Message = "Đăng nhập thành công",
            TenHienThi = user.TenHienThi ?? string.Empty,
            VaiTro = user.VaiTro ?? string.Empty,
            Token = token
        };

        return Ok(Result<LoginResponse>.Success("Đăng nhập thành công", response)
            );
    }

    private string GenerateToken(TaiKhoan user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim(ClaimTypes.Role, user.VaiTro ?? "NhanVien")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}