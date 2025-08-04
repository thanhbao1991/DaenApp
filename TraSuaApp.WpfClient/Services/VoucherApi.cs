using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class VoucherApi : BaseApi, IVoucherApi
{
    private const string BASE_URL = "/api/Voucher";

    public VoucherApi() : base(TuDien._tableFriendlyNames["Voucher"]) { }

    public async Task<Result<List<VoucherDto>>> GetAllAsync()
    {
        return await GetAsync<List<VoucherDto>>(BASE_URL);
    }

    public async Task<Result<VoucherDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<VoucherDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<VoucherDto>> CreateAsync(VoucherDto dto)
    {
        return await PostAsync<VoucherDto>(BASE_URL, dto);
    }

    public async Task<Result<VoucherDto>> UpdateAsync(Guid id, VoucherDto dto)
    {
        return await PutAsync<VoucherDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<VoucherDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<VoucherDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<VoucherDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<VoucherDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<VoucherDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<VoucherDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
