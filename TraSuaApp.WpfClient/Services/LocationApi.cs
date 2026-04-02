using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class LocationApi : BaseApi
{
    private const string BASE_URL = "/api/Location";

    public LocationApi() : base(TuDien._tableFriendlyNames["Location"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<LocationDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<LocationDto>>(BASE_URL, ct);
    }

    public async Task<Result<LocationDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<LocationDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<LocationDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<LocationDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<LocationDto>> CreateAsync(LocationDto dto, CancellationToken ct = default)
    {
        return await PostAsync<LocationDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<LocationDto>> UpdateAsync(Guid id, LocationDto dto, CancellationToken ct = default)
    {
        return await PutAsync<LocationDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<LocationDto>> UpdateSingleAsync(Guid id, LocationDto dto, CancellationToken ct = default)
    {
        return await PutAsync<LocationDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<LocationDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<LocationDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<LocationDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<LocationDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}