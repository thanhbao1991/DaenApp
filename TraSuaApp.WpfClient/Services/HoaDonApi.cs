using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class HoaDonApi : BaseApi, IHoaDonApi
{
    private const string BASE_URL = "/api/HoaDon";

    public HoaDonApi() : base(TuDien._tableFriendlyNames["HoaDon"]) { }

    public async Task<Result<List<HoaDonDto>>> GetAllAsync()
    {
        var url = BASE_URL; // /api/HoaDon

        var username = Properties.Settings.Default.TaiKhoan;

        if (!string.IsNullOrWhiteSpace(username) &&
            username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            url = $"{BASE_URL}/for-admin";
        }

        return await GetAsync<List<HoaDonDto>>(url);
    }

    //public async Task<Result<List<HoaDonDto>>> GetAllAsync()
    //{
    //    return await GetAsync<List<HoaDonDto>>(BASE_URL);
    //}

    public async Task<Result<HoaDonDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<HoaDonDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<HoaDonDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        return await PostAsync<HoaDonDto>(BASE_URL, dto);
    }

    public async Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}", dto);
    }
    public async Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/single", dto);
    }
    public async Task<Result<HoaDonDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<HoaDonDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<HoaDonDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/restore", null!);
    }
}
