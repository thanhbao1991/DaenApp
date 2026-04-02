using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SanPhamBienTheApi : BaseApi
{
    private const string BASE_URL = "/api/SanPhamBienThe";

    public SanPhamBienTheApi() : base(TuDien._tableFriendlyNames["SanPhamBienThe"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<SanPhamBienTheDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<SanPhamBienTheDto>>(BASE_URL, ct);
    }

    public async Task<Result<SanPhamBienTheDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<SanPhamBienTheDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<SanPhamBienTheDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<SanPhamBienTheDto>> CreateAsync(SanPhamBienTheDto dto, CancellationToken ct = default)
    {
        return await PostAsync<SanPhamBienTheDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<SanPhamBienTheDto>> UpdateAsync(Guid id, SanPhamBienTheDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<SanPhamBienTheDto>> UpdateSingleAsync(Guid id, SanPhamBienTheDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<SanPhamBienTheDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<SanPhamBienTheDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}