using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public class HoaDonApi : BaseApi
{
    private const string BASE_URL = "/api/HoaDon";

    public HoaDonApi() : base(TuDien._tableFriendlyNames["HoaDon"]) { }

    // =========================
    // GET
    // =========================
    public async Task<Result<List<HoaDonDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await GetAsync<List<HoaDonDto>>(BASE_URL, ct);
    }

    public async Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
    {
        return await GetAsync<List<HoaDonDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
            ct);
    }

    public async Task<Result<HoaDonDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<HoaDonDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<KhachHangInfoDto>> GetKhachHangInfoAsync(Guid khachHangId, CancellationToken ct = default)
    {
        return await GetAsync<KhachHangInfoDto>($"{BASE_URL}/get-khach-hang-info/{khachHangId}", ct);
    }

    // =========================
    // CREATE / UPDATE
    // =========================
    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto, CancellationToken ct = default)
    {
        return await PostAsync<HoaDonDto>(BASE_URL, dto, ct);
    }

    public async Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}", dto, ct);
    }

    public async Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/single", dto, ct);
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task<Result<HoaDonDto>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteAsync<HoaDonDto>($"{BASE_URL}/{id}", ct);
    }

    public async Task<Result<HoaDonDto>> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/restore", null!, ct);
    }

    // =========================
    // FAST UPDATE
    // =========================
    public async Task<Result<HoaDonNoDto>> UpdateEscSingleAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/esc", dto, ct);
    }

    public async Task<Result<HoaDonNoDto>> UpdateRollBackSingleAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/rollback", dto, ct);
    }

    public async Task<Result<HoaDonNoDto>> UpdatePrintSingleAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/print", dto, ct);
    }

    public async Task<Result<HoaDonNoDto>> UpdateF12SingleAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f12", dto, ct);
    }

    public async Task<Result<HoaDonNoDto>> UpdateF1F4SingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f1f4", dto, ct);
    }
}