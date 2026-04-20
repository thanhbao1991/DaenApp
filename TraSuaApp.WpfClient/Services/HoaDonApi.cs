using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.WpfClient.Services;

public class HoaDonApi : BaseApi<HoaDonDto>
{
    private const string BASE_URL = "/api/HoaDon";

    public HoaDonApi()
        : base(BASE_URL, TuDien._tableFriendlyNames["HoaDon"])
    {
    }

    // =========================
    // CUSTOM GET
    // =========================
    public Task<Result<KhachHangInfoDto>> GetKhachHangInfoAsync(
        Guid khachHangId,
        CancellationToken ct = default)
        => GetAsync<KhachHangInfoDto>(
            $"{BASE_URL}/get-khach-hang-info/{khachHangId}", ct);

    // =========================
    // FAST UPDATE
    // =========================
    public Task<Result<HoaDonNoDto>> EscAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/esc", dto, ct);

    public Task<Result<HoaDonNoDto>> RollbackAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/rollback", dto, ct);

    public Task<Result<HoaDonNoDto>> PrintAsync(Guid id, HoaDonDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/print", dto, ct);

    public Task<Result<HoaDonNoDto>> F12Async(Guid id, HoaDonDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f12", dto, ct);

    public Task<Result<HoaDonNoDto>> F1Async(Guid id, ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f1", dto, ct);

    public Task<Result<HoaDonNoDto>> F4Async(Guid id, ChiTietHoaDonThanhToanDto dto, CancellationToken ct = default)
        => PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f4", dto, ct);
}