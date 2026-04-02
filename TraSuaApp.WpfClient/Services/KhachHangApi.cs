using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class KhachHangApi : BaseApi
{
    private const string BASE_URL = "/api/KhachHang";

    public KhachHangApi() : base(TuDien._tableFriendlyNames["KhachHang"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<KhachHangDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<KhachHangDto>>(BASE_URL, ct);
    }

    public async Task<Result<KhachHangDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<KhachHangDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<KhachHangDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<KhachHangDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto, CancellationToken ct = default)
    {
        return await PostAsync<KhachHangDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<KhachHangDto>> UpdateSingleAsync(Guid id, KhachHangDto dto, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<KhachHangDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<KhachHangDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<KhachHangDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}