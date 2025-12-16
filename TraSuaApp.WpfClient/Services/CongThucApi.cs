using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class CongThucApi : BaseApi, ICongThucApi
{
    private const string BASE_URL = "/api/CongThuc";

    public CongThucApi() : base(TuDien._tableFriendlyNames["CongThuc"]) { }

    public async Task<Result<List<CongThucDto>>> GetAllAsync()
    {
        return await GetAsync<List<CongThucDto>>(BASE_URL);
    }

    public async Task<Result<CongThucDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<CongThucDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<CongThucDto>> CreateAsync(CongThucDto dto)
    {
        return await PostAsync<CongThucDto>(BASE_URL, dto);
    }

    public async Task<Result<CongThucDto>> UpdateAsync(Guid id, CongThucDto dto)
    {
        // ✅ PUT để match [HttpPut("{id}")]
        return await PutAsync<CongThucDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<CongThucDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<CongThucDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<CongThucDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<CongThucDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<CongThucDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<CongThucDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}