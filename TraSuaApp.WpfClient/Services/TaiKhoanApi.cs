using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class TaiKhoanApi : BaseApi
{
    private const string BASE_URL = "/api/TaiKhoan";

    public TaiKhoanApi() : base(TuDien._tableFriendlyNames["TaiKhoan"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<TaiKhoanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<TaiKhoanDto>>(BASE_URL, ct);
    }

    public async Task<Result<TaiKhoanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<TaiKhoanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<TaiKhoanDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<TaiKhoanDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto, CancellationToken ct = default)
    {
        return await PostAsync<TaiKhoanDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<TaiKhoanDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<TaiKhoanDto>> UpdateSingleAsync(Guid id, TaiKhoanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<TaiKhoanDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<TaiKhoanDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<TaiKhoanDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<TaiKhoanDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}