using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SuDungNguyenLieuApi : BaseApi
{
    private const string BASE_URL = "/api/SuDungNguyenLieu";

    public SuDungNguyenLieuApi() : base(TuDien._tableFriendlyNames["SuDungNguyenLieu"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<SuDungNguyenLieuDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<SuDungNguyenLieuDto>>(BASE_URL, ct);
    }

    public async Task<Result<SuDungNguyenLieuDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<SuDungNguyenLieuDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<SuDungNguyenLieuDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<SuDungNguyenLieuDto>> CreateAsync(SuDungNguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PostAsync<SuDungNguyenLieuDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<SuDungNguyenLieuDto>> UpdateAsync(Guid id, SuDungNguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<SuDungNguyenLieuDto>> UpdateSingleAsync(Guid id, SuDungNguyenLieuDto dto, CancellationToken ct = default)
    {
        return await PutAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<SuDungNguyenLieuDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<SuDungNguyenLieuDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}