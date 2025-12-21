using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ChiTieuHangNgayApi : BaseApi, IChiTieuHangNgayApi
{
    private const string BASE_URL = "/api/ChiTieuHangNgay";

    public ChiTieuHangNgayApi() : base(TuDien._tableFriendlyNames["ChiTieuHangNgay"]) { }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetAllAsync()
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>(BASE_URL);
    }

    public async Task<Result<ChiTieuHangNgayDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}");
    }
    public async Task<Result<List<ChiTieuHangNgayDto>>> CreateBulkAsync(
    ChiTieuHangNgayBulkCreateDto dto)
    {
        return await PostAsync<List<ChiTieuHangNgayDto>>(
            $"{BASE_URL}/bulk", dto);
    }

    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto)
    {
        return await PostAsync<ChiTieuHangNgayDto>(BASE_URL, dto);
    }

    public async Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto)
    {
        return await PutAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ChiTieuHangNgayDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<ChiTieuHangNgayDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
    public async Task<Result<List<ChiTieuHangNgayDto>>> GetByNguyenLieuInMonth(int year, int month)
    {
        return await GetAsync<List<ChiTieuHangNgayDto>>(
            $"{BASE_URL}/nguyenlieu/{year}/{month}"
        );
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetThisMonth()
    {
        var dt = DateTime.Today;
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetLastMonth()
    {
        var dt = DateTime.Today.AddMonths(-1);
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month);
    }

    public async Task<Result<List<ChiTieuHangNgayDto>>> GetTwoMonthsAgo()
    {
        var dt = DateTime.Today.AddMonths(-2);
        return await GetByNguyenLieuInMonth(dt.Year, dt.Month);
    }
}
