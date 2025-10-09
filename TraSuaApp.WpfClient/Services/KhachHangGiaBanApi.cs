using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class KhachHangGiaBanApi : BaseApi, IKhachHangGiaBanApi
{
    private const string BASE_URL = "/api/KhachHangGiaBan";

    public KhachHangGiaBanApi() : base(TuDien._tableFriendlyNames["KhachHangGiaBan"]) { }

    public async Task<Result<List<KhachHangGiaBanDto>>> GetAllAsync()
    {
        return await GetAsync<List<KhachHangGiaBanDto>>(BASE_URL);
    }

    public async Task<Result<KhachHangGiaBanDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<KhachHangGiaBanDto>> CreateAsync(KhachHangGiaBanDto dto)
    {
        return await PostAsync<KhachHangGiaBanDto>(BASE_URL, dto);
    }

    public async Task<Result<KhachHangGiaBanDto>> UpdateAsync(Guid id, KhachHangGiaBanDto dto)
    {
        return await PutAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<KhachHangGiaBanDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<KhachHangGiaBanDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<KhachHangGiaBanDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<KhachHangGiaBanDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<KhachHangGiaBanDto>>($"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }
}
