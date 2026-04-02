using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SanPhamApi : BaseApi
{
    private const string BASE_URL = "/api/SanPham";

    public SanPhamApi() : base(TuDien._tableFriendlyNames["SanPham"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<SanPhamDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<SanPhamDto>>(BASE_URL, ct);
    }

    public async Task<Result<SanPhamDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<SanPhamDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<SanPhamDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<SanPhamDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto, CancellationToken ct = default)
    {
        return await PostAsync<SanPhamDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<SanPhamDto>> UpdateSingleAsync(Guid id, SanPhamDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<SanPhamDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<SanPhamDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<SanPhamDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<SanPhamDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}