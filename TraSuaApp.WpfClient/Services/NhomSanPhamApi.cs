using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NhomSanPhamApi : BaseApi
{
    private const string BASE_URL = "/api/NhomSanPham";

    public NhomSanPhamApi() : base(TuDien._tableFriendlyNames["NhomSanPham"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<NhomSanPhamDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<NhomSanPhamDto>>(BASE_URL, ct);
    }

    public async Task<Result<NhomSanPhamDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<NhomSanPhamDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<List<NhomSanPhamDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<NhomSanPhamDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto, CancellationToken ct = default)
    {
        return await PostAsync<NhomSanPhamDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NhomSanPhamDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<NhomSanPhamDto>> UpdateSingleAsync(Guid id, NhomSanPhamDto dto, CancellationToken ct = default)
    {
        return await PutAsync<NhomSanPhamDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<NhomSanPhamDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<NhomSanPhamDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }
}