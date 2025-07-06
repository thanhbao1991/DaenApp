using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TraSuaApp.WpfClient.Helpers;

public static class JwtHelper
{
    public static string? GetRole(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
    }

    public static string? GetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(x => x.Type == "Id")?.Value;
    }
}