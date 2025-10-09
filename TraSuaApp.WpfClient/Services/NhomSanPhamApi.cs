using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NhomSanPhamApi : BaseApi, INhomSanPhamApi
{
    private const string BASE_URL = "/api/NhomSanPham";

    public NhomSanPhamApi() : base(TuDien._tableFriendlyNames["NhomSanPham"]) { }

    public async Task<Result<List<NhomSanPhamDto>>> GetAllAsync()
    {
        return await GetAsync<List<NhomSanPhamDto>>(BASE_URL);
    }

    public async Task<Result<NhomSanPhamDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<NhomSanPhamDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto)
    {
        return await PostAsync<NhomSanPhamDto>(BASE_URL, dto);
    }

    public async Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        return await PutAsync<NhomSanPhamDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<NhomSanPhamDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<NhomSanPhamDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<NhomSanPhamDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<NhomSanPhamDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
