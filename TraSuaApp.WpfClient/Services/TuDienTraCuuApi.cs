using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class TuDienTraCuuApi : BaseApi, ITuDienTraCuuApi
{
    private const string BASE_URL = "/api/TuDienTraCuu";

    public TuDienTraCuuApi() : base(TuDien._tableFriendlyNames["TuDienTraCuu"]) { }

    public async Task<Result<List<TuDienTraCuuDto>>> GetAllAsync()
    {
        return await GetAsync<List<TuDienTraCuuDto>>(BASE_URL);
    }

    public async Task<Result<TuDienTraCuuDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<TuDienTraCuuDto>> CreateAsync(TuDienTraCuuDto dto)
    {
        return await PostAsync<TuDienTraCuuDto>(BASE_URL, dto);
    }

    public async Task<Result<TuDienTraCuuDto>> UpdateAsync(Guid id, TuDienTraCuuDto dto)
    {
        return await PostAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<TuDienTraCuuDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<TuDienTraCuuDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<TuDienTraCuuDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<TuDienTraCuuDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
