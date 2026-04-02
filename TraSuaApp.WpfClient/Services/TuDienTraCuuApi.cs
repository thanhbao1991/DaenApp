using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class TuDienTraCuuApi : BaseApi
{
    private const string BASE_URL = "/api/TuDienTraCuu";

    public TuDienTraCuuApi() : base(TuDien._tableFriendlyNames["TuDienTraCuu"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<TuDienTraCuuDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<TuDienTraCuuDto>>(BASE_URL, ct);
    }

    public async Task<Result<TuDienTraCuuDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<TuDienTraCuuDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<TuDienTraCuuDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<TuDienTraCuuDto>> CreateAsync(TuDienTraCuuDto dto, CancellationToken ct = default)
    {
        return await PostAsync<TuDienTraCuuDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<TuDienTraCuuDto>> UpdateAsync(Guid id, TuDienTraCuuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<TuDienTraCuuDto>> UpdateSingleAsync(Guid id, TuDienTraCuuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<TuDienTraCuuDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<TuDienTraCuuDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<TuDienTraCuuDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}