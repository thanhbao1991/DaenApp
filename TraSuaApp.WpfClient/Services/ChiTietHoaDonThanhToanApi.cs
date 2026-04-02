using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ChiTietHoaDonThanhToanApi : BaseApi
{
    private const string BASE_URL = "/api/ChiTietHoaDonThanhToan";

    public ChiTietHoaDonThanhToanApi()
        : base(TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<ChiTietHoaDonThanhToanDto>>(BASE_URL, ct);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<ChiTietHoaDonThanhToanDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
    {
        return await PostAsync<ChiTietHoaDonThanhToanDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateSingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}