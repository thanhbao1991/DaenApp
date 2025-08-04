using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ToppingApi : BaseApi, IToppingApi
{
    private const string BASE_URL = "/api/Topping";

    public ToppingApi() : base(TuDien._tableFriendlyNames["Topping"]) { }

    public async Task<Result<List<ToppingDto>>> GetAllAsync()
    {
        return await GetAsync<List<ToppingDto>>(BASE_URL);
    }

    public async Task<Result<ToppingDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<ToppingDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ToppingDto>> CreateAsync(ToppingDto dto)
    {
        return await PostAsync<ToppingDto>(BASE_URL, dto);
    }

    public async Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto)
    {
        return await PutAsync<ToppingDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<ToppingDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<ToppingDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ToppingDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<ToppingDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<ToppingDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<ToppingDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
