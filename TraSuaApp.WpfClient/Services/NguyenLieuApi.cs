using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuApi : BaseApi, INguyenLieuApi
{
    private const string BASE_URL = "/api/NguyenLieu";

    public NguyenLieuApi() : base(TuDien._tableFriendlyNames["NguyenLieu"]) { }

    public async Task<Result<List<NguyenLieuDto>>> GetAllAsync()
    {
        return await GetAsync<List<NguyenLieuDto>>(BASE_URL);
    }

    public async Task<Result<NguyenLieuDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<NguyenLieuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuDto>> CreateAsync(NguyenLieuDto dto)
    {
        return await PostAsync<NguyenLieuDto>(BASE_URL, dto);
    }

    public async Task<Result<NguyenLieuDto>> UpdateAsync(Guid id, NguyenLieuDto dto)
    {
        return await PutAsync<NguyenLieuDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<NguyenLieuDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<NguyenLieuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<NguyenLieuDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<NguyenLieuDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<NguyenLieuDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
