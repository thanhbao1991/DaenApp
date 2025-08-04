using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services;

public class PhuongThucThanhToanApi : BaseApi, IPhuongThucThanhToanApi
{
    private const string BASE_URL = "/api/PhuongThucThanhToan";

    public PhuongThucThanhToanApi() : base(TuDien._tableFriendlyNames["PhuongThucThanhToan"]) { }

    public async Task<Result<List<PhuongThucThanhToanDto>>> GetAllAsync()
    {
        return await GetAsync<List<PhuongThucThanhToanDto>>(BASE_URL);
    }

    public async Task<Result<PhuongThucThanhToanDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto)
    {
        return await PostAsync<PhuongThucThanhToanDto>(BASE_URL, dto);
    }

    public async Task<Result<PhuongThucThanhToanDto>> UpdateAsync(Guid id, PhuongThucThanhToanDto dto)
    {
        return await PutAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id)
    {
        return await PostAsync<PhuongThucThanhToanDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<List<PhuongThucThanhToanDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<PhuongThucThanhToanDto>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
    }
}
