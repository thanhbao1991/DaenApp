using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuBanHangApi : BaseApi, INguyenLieuBanHangApi
{
    private const string BASE_URL = "/api/NguyenLieuBanHang";

    public NguyenLieuBanHangApi() : base(TuDien._tableFriendlyNames["NguyenLieuBanHang"]) { }

    public async Task<Result<List<NguyenLieuBanHangDto>>> GetAllAsync()
    {
        return await GetAsync<List<NguyenLieuBanHangDto>>(BASE_URL);
    }

    public async Task<Result<NguyenLieuBanHangDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuBanHangDto>> CreateAsync(NguyenLieuBanHangDto dto)
    {
        return await PostAsync<NguyenLieuBanHangDto>(BASE_URL, dto);
    }

    // TraSuaApp.WpfClient.Services/NguyenLieuBanHangApi.cs

    public async Task<Result<NguyenLieuBanHangDto>> UpdateAsync(Guid id, NguyenLieuBanHangDto dto)
    {
        // ❌ đang sai: PostAsync
        // return await PostAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}", dto);

        // ✅ đúng: PutAsync (match với [HttpPut("{id}")] bên Controller)
        return await PutAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}", dto);
    }
    public async Task<Result<NguyenLieuBanHangDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuBanHangDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<NguyenLieuBanHangDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<NguyenLieuBanHangDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<NguyenLieuBanHangDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
