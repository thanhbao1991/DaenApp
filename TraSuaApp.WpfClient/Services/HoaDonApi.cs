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
        return await GetAsync<List<HoaDonDto>>(BASE_URL);
    }

    public async Task<Result<HoaDonDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<HoaDonDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<HoaDonDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
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
        return await PostAsync<HoaDonDto>($"{BASE_URL}/{id}/restore", null!);
    }
}
