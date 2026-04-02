using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class PhuongThucThanhToanApi : BaseApi
{
    private const string BASE_URL = "/api/PhuongThucThanhToan";

    public PhuongThucThanhToanApi() : base(TuDien._tableFriendlyNames["PhuongThucThanhToan"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<PhuongThucThanhToanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<PhuongThucThanhToanDto>>(BASE_URL, ct);
    }

    public async Task<Result<PhuongThucThanhToanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<PhuongThucThanhToanDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<PhuongThucThanhToanDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto, CancellationToken ct = default)
    {
        return await PostAsync<PhuongThucThanhToanDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<PhuongThucThanhToanDto>> UpdateAsync(Guid id, PhuongThucThanhToanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<PhuongThucThanhToanDto>> UpdateSingleAsync(Guid id, PhuongThucThanhToanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}