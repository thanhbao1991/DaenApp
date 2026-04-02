using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuApi : BaseApi
{
    private const string BASE_URL = "/api/NguyenLieu";

    public NguyenLieuApi() : base(TuDien._tableFriendlyNames["NguyenLieu"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<NguyenLieuDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuDto>>(BASE_URL, ct);
    }

    public async Task<Result<NguyenLieuDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<NguyenLieuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<NguyenLieuDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<NguyenLieuDto>> CreateAsync(NguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PostAsync<NguyenLieuDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<NguyenLieuDto>> UpdateAsync(Guid id, NguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<NguyenLieuDto>> UpdateSingleAsync(Guid id, NguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<NguyenLieuDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<NguyenLieuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<NguyenLieuDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}