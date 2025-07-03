
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}