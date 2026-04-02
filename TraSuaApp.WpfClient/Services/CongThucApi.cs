using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class CongThucApi : BaseApi
{
    private const string BASE_URL = "/api/CongThuc";

    public CongThucApi()
        : base(TuDien._tableFriendlyNames["CongThuc"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<CongThucDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<CongThucDto>>(BASE_URL, ct);
    }

    public async Task<Result<CongThucDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<CongThucDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<CongThucDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<CongThucDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<CongThucDto>> CreateAsync(CongThucDto dto, CancellationToken ct = default)
    {
        return await PostAsync<CongThucDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<CongThucDto>> UpdateAsync(Guid id, CongThucDto dto, CancellationToken ct = default)
    {
        return await PutAsync<CongThucDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<CongThucDto>> UpdateSingleAsync(Guid id, CongThucDto dto, CancellationToken ct = default)
    {
        return await PutAsync<CongThucDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<CongThucDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<CongThucDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<CongThucDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<CongThucDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}