using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuBanHangApi : BaseApi
{
    private const string BASE_URL = "/api/NguyenLieuBanHang";

    public NguyenLieuBanHangApi() : base(TuDien._tableFriendlyNames["NguyenLieuBanHang"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<NguyenLieuBanHangDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuBanHangDto>>(BASE_URL, ct);
    }

    public async Task<Result<NguyenLieuBanHangDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<NguyenLieuBanHangDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuBanHangDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<NguyenLieuBanHangDto>> CreateAsync(NguyenLieuBanHangDto dto, CancellationToken ct = default)
    {
        return await PostAsync<NguyenLieuBanHangDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<NguyenLieuBanHangDto>> UpdateAsync(Guid id, NguyenLieuBanHangDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<NguyenLieuBanHangDto>> UpdateSingleAsync(Guid id, NguyenLieuBanHangDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<NguyenLieuBanHangDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<NguyenLieuBanHangDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}