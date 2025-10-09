using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ChiTietHoaDonThanhToanApi : BaseApi, IChiTietHoaDonThanhToanApi
{
    private const string BASE_URL = "/api/ChiTietHoaDonThanhToan";

    public ChiTietHoaDonThanhToanApi() : base(TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"]) { }

    public async Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetAllAsync()
    {
        return await GetAsync<List<ChiTietHoaDonThanhToanDto>>(BASE_URL);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto)
    {
        return await PostAsync<ChiTietHoaDonThanhToanDto>(BASE_URL, dto);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        return await PutAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<ChiTietHoaDonThanhToanDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<ChiTietHoaDonThanhToanDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<ChiTietHoaDonThanhToanDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
