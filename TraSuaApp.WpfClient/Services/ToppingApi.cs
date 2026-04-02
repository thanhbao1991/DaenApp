using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ToppingApi : BaseApi
{
    private const string BASE_URL = "/api/Topping";

    public ToppingApi() : base(TuDien._tableFriendlyNames["Topping"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<ToppingDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<ToppingDto>>(BASE_URL, ct);
    }

    public async Task<Result<ToppingDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<ToppingDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<ToppingDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<ToppingDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<ToppingDto>> CreateAsync(ToppingDto dto, CancellationToken ct = default)
    {
        return await PostAsync<ToppingDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ToppingDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<ToppingDto>> UpdateSingleAsync(Guid id, ToppingDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ToppingDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<ToppingDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<ToppingDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<ToppingDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<ToppingDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}