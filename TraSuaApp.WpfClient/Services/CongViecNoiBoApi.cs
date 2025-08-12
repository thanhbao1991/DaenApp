using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class CongViecNoiBoApi : BaseApi, ICongViecNoiBoApi
{
    private const string BASE_URL = "/api/CongViecNoiBo";

    public CongViecNoiBoApi() : base(TuDien._tableFriendlyNames["CongViecNoiBo"]) { }

    public async Task<Result<List<CongViecNoiBoDto>>> GetAllAsync()
    {
        return await GetAsync<List<CongViecNoiBoDto>>(BASE_URL);
    }

    public async Task<Result<CongViecNoiBoDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<CongViecNoiBoDto>> CreateAsync(CongViecNoiBoDto dto)
    {
        return await PostAsync<CongViecNoiBoDto>(BASE_URL, dto);
    }

    public async Task<Result<CongViecNoiBoDto>> UpdateAsync(Guid id, CongViecNoiBoDto dto)
    {
        return await PutAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<CongViecNoiBoDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<CongViecNoiBoDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<CongViecNoiBoDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<CongViecNoiBoDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<CongViecNoiBoDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
