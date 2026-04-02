using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class KhachHangGiaBanApi : BaseApi
{
    private const string BASE_URL = "/api/KhachHangGiaBan";

    public KhachHangGiaBanApi()
        : base(TuDien._tableFriendlyNames["KhachHangGiaBan"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<KhachHangGiaBanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<KhachHangGiaBanDto>>(BASE_URL, ct);
    }

    public async Task<Result<KhachHangGiaBanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<KhachHangGiaBanDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<KhachHangGiaBanDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<KhachHangGiaBanDto>> CreateAsync(KhachHangGiaBanDto dto, CancellationToken ct = default)
    {
        return await PostAsync<KhachHangGiaBanDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<KhachHangGiaBanDto>> UpdateAsync(Guid id, KhachHangGiaBanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<KhachHangGiaBanDto>> UpdateSingleAsync(Guid id, KhachHangGiaBanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<KhachHangGiaBanDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<KhachHangGiaBanDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}