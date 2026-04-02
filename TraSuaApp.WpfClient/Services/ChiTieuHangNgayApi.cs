using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ChiTieuHangNgayApi : BaseApi
{
    private const string BASE_URL = "/api/ChiTieuHangNgay";

    public ChiTieuHangNgayApi()
        : base(TuDien._tableFriendlyNames["ChiTieuHangNgay"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<ChiTieuHangNgayDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>(BASE_URL, ct);
    }

    public async Task<Result<ChiTieuHangNgayDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto, CancellationToken ct = default)
    {
        return await PostAsync<ChiTieuHangNgayDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> CreateBulkAsync(
        ChiTieuHangNgayBulkCreateDto dto,
        CancellationToken ct = default)
    {
        return await PostAsync<List<ChiTieuHangNgayDto>>(
            $"{BASE_URL}/bulk",
            dto,
            ct);
    }

    public async Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<ChiTieuHangNgayDto>> UpdateSingleAsync(Guid id, ChiTieuHangNgayDto dto, CancellationToken ct = default)
    {
        return await PutAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // CUSTOM QUERY
    // =========================
    public async Task<Result<List<ChiTieuHangNgayDto>>> GetByNguyenLieuInMonth(
        int year,
        int month,
        CancellationToken ct = default)
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>(
            $"{BASE_URL}/nguyenlieu/{year}/{month}",
            ct);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetThisMonth(CancellationToken ct = default)
    {
        var dt = DateTime.Today;
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month, ct);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetLastMonth(CancellationToken ct = default)
    {
        var dt = DateTime.Today.AddMonths(-1);
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month, ct);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetTwoMonthsAgo(CancellationToken ct = default)
    {
        var dt = DateTime.Today.AddMonths(-2);
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<ChiTieuHangNgayDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}