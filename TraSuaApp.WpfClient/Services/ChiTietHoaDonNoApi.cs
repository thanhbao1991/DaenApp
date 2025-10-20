using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class ChiTietHoaDonNoApi : BaseApi, IChiTietHoaDonNoApi
{
    private const string BASE_URL = "/api/ChiTietHoaDonNo";

    public ChiTietHoaDonNoApi() : base(TuDien._tableFriendlyNames["ChiTietHoaDonNo"]) { }

    public async Task<Result<List<ChiTietHoaDonNoDto>>> GetAllAsync()
    {
        return await GetAsync<List<ChiTietHoaDonNoDto>>(BASE_URL);
    }
    public async Task<Result<ChiTietHoaDonThanhToanDto>> PayAsync(Guid id, string type)
    {
        var res = await PostAsync<ChiTietHoaDonThanhToanDto>($"/api/ChiTietHoaDonNo/{id}/pay",
            new TraSuaApp.Shared.Dtos.Requests.PayDebtRequest { Type = type });
        return res;
    }
    public async Task<Result<ChiTietHoaDonNoDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<ChiTietHoaDonNoDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ChiTietHoaDonNoDto>> CreateAsync(ChiTietHoaDonNoDto dto)
    {
        return await PostAsync<ChiTietHoaDonNoDto>(BASE_URL, dto);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> UpdateAsync(Guid id, ChiTietHoaDonNoDto dto)
    {
        return await PutAsync<ChiTietHoaDonNoDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<ChiTietHoaDonNoDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<ChiTietHoaDonNoDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<ChiTietHoaDonNoDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<ChiTietHoaDonNoDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<ChiTietHoaDonNoDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
