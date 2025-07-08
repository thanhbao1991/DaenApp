using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                Message = result.Message,
                Data = result.Data // chứa LoginResponse (TenHienThi, VaiTro, Token...)
            });
        }

        return Unauthorized(new
        {
            Message = result.Message
        });
    }
}