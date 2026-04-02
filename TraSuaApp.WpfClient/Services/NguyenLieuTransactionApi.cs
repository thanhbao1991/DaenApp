using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuTransactionApi : BaseApi
{
    private const string BASE_URL = "/api/NguyenLieuTransaction";

    public NguyenLieuTransactionApi() : base(TuDien._tableFriendlyNames["NguyenLieuTransaction"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<NguyenLieuTransactionDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuTransactionDto>>(BASE_URL, ct);
    }

    public async Task<Result<NguyenLieuTransactionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<NguyenLieuTransactionDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<NguyenLieuTransactionDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto, CancellationToken ct = default)
    {
        return await PostAsync<NguyenLieuTransactionDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<NguyenLieuTransactionDto>> UpdateSingleAsync(Guid id, NguyenLieuTransactionDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}