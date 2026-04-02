using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class VoucherApi : BaseApi
{
    private const string BASE_URL = "/api/Voucher";

    public VoucherApi() : base(TuDien._tableFriendlyNames["Voucher"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<VoucherDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<VoucherDto>>(BASE_URL, ct);
    }

    public async Task<Result<VoucherDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<VoucherDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<VoucherDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<VoucherDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<VoucherDto>> CreateAsync(VoucherDto dto, CancellationToken ct = default)
    {
        return await PostAsync<VoucherDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<VoucherDto>> UpdateAsync(Guid id, VoucherDto dto, CancellationToken ct = default)
    {
        return await PutAsync<VoucherDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<VoucherDto>> UpdateSingleAsync(Guid id, VoucherDto dto, CancellationToken ct = default)
    {
        return await PutAsync<VoucherDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<VoucherDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<VoucherDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<VoucherDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<VoucherDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }

    // =========================
    // CUSTOM
    // =========================
    public Task<Result<List<VoucherChiTraDto>>> GetByOffset(int offset, CancellationToken ct = default)
        => GetAsync<List<VoucherChiTraDto>>($"{BASE_URL}/by-offset?offset={offset}", ct);

    public Task<Result<List<VoucherChiTraDto>>> GetThisMonth(CancellationToken ct = default)
        => GetByOffset(0, ct);

    public Task<Result<List<VoucherChiTraDto>>> GetLastMonth(CancellationToken ct = default)
        => GetByOffset(-1, ct);
}