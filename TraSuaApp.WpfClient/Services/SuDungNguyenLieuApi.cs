using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class SuDungNguyenLieuApi : BaseApi, ISuDungNguyenLieuApi
{
    private const string BASE_URL = "/api/SuDungNguyenLieu";

    public SuDungNguyenLieuApi() : base(TuDien._tableFriendlyNames["SuDungNguyenLieu"]) { }

    public async Task<Result<List<SuDungNguyenLieuDto>>> GetAllAsync()
    {
        return await GetAsync<List<SuDungNguyenLieuDto>>(BASE_URL);
    }

    public async Task<Result<SuDungNguyenLieuDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SuDungNguyenLieuDto>> CreateAsync(SuDungNguyenLieuDto dto)
    {
        return await PostAsync<SuDungNguyenLieuDto>(BASE_URL, dto);
    }

    public async Task<Result<SuDungNguyenLieuDto>> UpdateAsync(Guid id, SuDungNguyenLieuDto dto)
    {
        // ✅ PUT để match [HttpPut("{id}")]
        return await PutAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<SuDungNguyenLieuDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<SuDungNguyenLieuDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<SuDungNguyenLieuDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<SuDungNguyenLieuDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<SuDungNguyenLieuDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}