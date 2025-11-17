using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class LocationApi : BaseApi, ILocationApi
{
    private const string BASE_URL = "/api/Location";

    public LocationApi() : base(TuDien._tableFriendlyNames["Location"]) { }

    public async Task<Result<List<LocationDto>>> GetAllAsync()
    {
        return await GetAsync<List<LocationDto>>(BASE_URL);
    }

    public async Task<Result<LocationDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<LocationDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<LocationDto>> CreateAsync(LocationDto dto)
    {
        return await PostAsync<LocationDto>(BASE_URL, dto);
    }

    public async Task<Result<LocationDto>> UpdateAsync(Guid id, LocationDto dto)
    {
        return await PutAsync<LocationDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<LocationDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<LocationDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<LocationDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<LocationDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<LocationDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<LocationDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
