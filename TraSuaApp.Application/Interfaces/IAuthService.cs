
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IAuthService
{
    Task<Result> LoginAsync(LoginRequest request);
}