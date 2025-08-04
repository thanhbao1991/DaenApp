using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class KhachHangApi : BaseApi, IKhachHangApi
{
    private const string BASE_URL = "/api/KhachHang";

    public KhachHangApi() : base(TuDien._tableFriendlyNames["KhachHang"]) { }

    public async Task<Result<List<KhachHangDto>>> GetAllAsync()
    {
        return await GetAsync<List<KhachHangDto>>(BASE_URL);
    }

    public async Task<Result<KhachHangDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<KhachHangDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto)
    {
        return await PostAsync<KhachHangDto>(BASE_URL, dto);
    }

    public async Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto)
    {
        return await PutAsync<KhachHangDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<KhachHangDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<KhachHangDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<KhachHangDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<KhachHangDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<KhachHangDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<KhachHangDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
