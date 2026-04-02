using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class CongViecNoiBoApi : BaseApi
{
    private const string BASE_URL = "/api/CongViecNoiBo";

    public CongViecNoiBoApi()
        : base(TuDien._tableFriendlyNames["CongViecNoiBo"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<CongViecNoiBoDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<CongViecNoiBoDto>>(BASE_URL, ct);
    }

    public async Task<Result<CongViecNoiBoDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<CongViecNoiBoDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<CongViecNoiBoDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<CongViecNoiBoDto>> CreateAsync(CongViecNoiBoDto dto, CancellationToken ct = default)
    {
        return await PostAsync<CongViecNoiBoDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<CongViecNoiBoDto>> UpdateAsync(Guid id, CongViecNoiBoDto dto, CancellationToken ct = default)
    {
        return await PutAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<CongViecNoiBoDto>> UpdateSingleAsync(Guid id, CongViecNoiBoDto dto, CancellationToken ct = default)
    {
        return await PutAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // CUSTOM ACTIONS
    // =========================
    public async Task<Result<CongViecNoiBoDto>> ToggleAsync(Guid id, CancellationToken ct = default)
    {
        return await PostAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}/toggle", null!, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<CongViecNoiBoDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<CongViecNoiBoDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}