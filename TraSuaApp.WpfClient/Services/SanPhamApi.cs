using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SanPhamApi : BaseApi, ISanPhamApi
{
    private const string BASE_URL = "/api/SanPham";

    public SanPhamApi() : base(TuDien._tableFriendlyNames["SanPham"]) { }

    public async Task<Result<List<SanPhamDto>>> GetAllAsync()
    {
        return await GetAsync<List<SanPhamDto>>(BASE_URL);
    }

    public async Task<Result<SanPhamDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<SanPhamDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto)
    {
        return await PostAsync<SanPhamDto>(BASE_URL, dto);
    }

    public async Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto)
    {
        return await PutAsync<SanPhamDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<SanPhamDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<SanPhamDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SanPhamDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<SanPhamDto>($"{BASE_URL}/{id}/restore", null!);
    }
    public async Task<Result<SanPhamDto>> UpdateSingleAsync(Guid id, SanPhamDto dto)
    {
        return await PutAsync<SanPhamDto>($"{BASE_URL}/{id}/single", dto);
    }
    public async Task<Result<List<SanPhamDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<SanPhamDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
