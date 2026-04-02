using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class DoanhThuApi : BaseApi
{
    private const string BASE_URL = "/api/DoanhThu";

    public DoanhThuApi()
        : base(TuDien._tableFriendlyNames["DoanhThu"]) { }

    // =========================
    // REPORT / QUERY
    // =========================
    public async Task<Result<List<DoanhThuNamItemDto>>> GetDoanhThuNam(
        int nam,
        CancellationToken ct = default)
    {
        return await GetAsync<List<DoanhThuNamItemDto>>(
            $"{BASE_URL}/nam?nam={nam}",
            ct);
    }

    public async Task<Result<List<DoanhThuThangItemDto>>> GetDoanhThuThang(
        int thang,
        int nam,
        CancellationToken ct = default)
    {
        return await GetAsync<List<DoanhThuThangItemDto>>(
            $"{BASE_URL}/thang?thang={thang}&nam={nam}",
            ct);
    }
}