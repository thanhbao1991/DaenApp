using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class TaiKhoanApi : BaseApi, ITaiKhoanApi
{
    private const string BASE_URL = "/api/TaiKhoan";

    public TaiKhoanApi() : base(TuDien._tableFriendlyNames["TaiKhoan"]) { }

    public async Task<Result<List<TaiKhoanDto>>> GetAllAsync()
    {
        return await GetAsync<List<TaiKhoanDto>>(BASE_URL);
    }

    public async Task<Result<TaiKhoanDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<TaiKhoanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto)
    {
        return await PostAsync<TaiKhoanDto>(BASE_URL, dto);
    }

    public async Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        return await PutAsync<TaiKhoanDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<TaiKhoanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<TaiKhoanDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<TaiKhoanDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<TaiKhoanDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<TaiKhoanDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
