using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class NguyenLieuTransactionApi : BaseApi, INguyenLieuTransactionApi
{
    private const string BASE_URL = "/api/NguyenLieuTransaction";

    public NguyenLieuTransactionApi() : base(TuDien._tableFriendlyNames["NguyenLieuTransaction"]) { }

    public async Task<Result<List<NguyenLieuTransactionDto>>> GetAllAsync()
    {
        return await GetAsync<List<NguyenLieuTransactionDto>>(BASE_URL);
    }

    public async Task<Result<NguyenLieuTransactionDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto)
    {
        return await PostAsync<NguyenLieuTransactionDto>(BASE_URL, dto);
    }

    public async Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto)
    {
        // ✅ PUT để match [HttpPut("{id}")]
        return await PutAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<NguyenLieuTransactionDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<NguyenLieuTransactionDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<NguyenLieuTransactionDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}