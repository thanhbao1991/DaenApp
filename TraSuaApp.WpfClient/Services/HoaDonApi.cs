using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public class HoaDonApi : BaseApi, IHoaDonApi
{
    private const string BASE_URL = "/api/HoaDon";

    public HoaDonApi() : base(TuDien._tableFriendlyNames["HoaDon"]) { }

    public async Task<Result<List<HoaDonDto>>> GetAllAsync()
    {
        return await GetAsync<List<HoaDonDto>>(BASE_URL);
    }

    public async Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since)
    {
        return await GetAsync<List<HoaDonDto>>(
            $"{BASE_URL}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}");
    }

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        return await PostAsync<HoaDonDto>(BASE_URL, dto);
    }

    public async Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}", dto);
    }

    public async Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/single", dto);
    }

    public async Task<Result<HoaDonDto>> DeleteAsync(Guid id)
    {
        return await DeleteAsync<HoaDonDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<HoaDonDto>> RestoreAsync(Guid id)
    {
        return await PutAsync<HoaDonDto>($"{BASE_URL}/{id}/restore", null!);
    }

    public async Task<Result<HoaDonDto>> GetByIdAsync(Guid id)
    {
        return await GetAsync<HoaDonDto>($"{BASE_URL}/{id}");
    }

    public async Task<Result<KhachHangInfoDto>> GetKhachHangInfoAsync(Guid khachHangId)
    {
        return await GetAsync<KhachHangInfoDto>($"{BASE_URL}/get-khach-hang-info/{khachHangId}");
    }

    // ===== FAST UPDATE (trả về HoaDonNoDto để patch list) =====

    public async Task<Result<HoaDonNoDto>> UpdateEscSingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/esc", dto);
    }

    public async Task<Result<HoaDonNoDto>> UpdateRollBackSingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/rollback", dto);
    }

    public async Task<Result<HoaDonNoDto>> UpdatePrintSingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/print", dto);
    }

    public async Task<Result<HoaDonNoDto>> UpdateF12SingleAsync(Guid id, HoaDonDto dto)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f12", dto);
    }
    public async Task<Result<HoaDonNoDto>> UpdateF1F4SingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        return await PutAsync<HoaDonNoDto>($"{BASE_URL}/{id}/f1f4", dto);
    }


}