using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SanPhamBienTheApi : BaseApi, ISanPhamBienTheApi
{
    private const string BASE_URL = "/api/SanPhamBienThe";

    public SanPhamBienTheApi() : base(TuDien._tableFriendlyNames["SanPhamBienThe"]) { }

    public async Task<Result<List<SanPhamBienTheDto>>> GetAllAsync()
    {
        return await GetAsync<List<SanPhamBienTheDto>>(BASE_URL);
    }

    public async Task<Result<SanPhamBienTheDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SanPhamBienTheDto>> CreateAsync(SanPhamBienTheDto dto)
    {
        return await PostAsync<SanPhamBienTheDto>(BASE_URL, dto);
    }

    public async Task<Result<SanPhamBienTheDto>> UpdateAsync(Guid id, SanPhamBienTheDto dto)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<SanPhamBienTheDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SanPhamBienTheDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}/restore", null!);
    }
    public async Task<Result<SanPhamBienTheDto>> UpdateSingleAsync(Guid id, SanPhamBienTheDto dto)
    {
        return await PutAsync<SanPhamBienTheDto>($"{BASE_URL}/{id}/single", dto);
    }
    public async Task<Result<List<SanPhamBienTheDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<SanPhamBienTheDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
