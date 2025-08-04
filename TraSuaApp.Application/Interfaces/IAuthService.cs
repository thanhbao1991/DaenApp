using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
}
