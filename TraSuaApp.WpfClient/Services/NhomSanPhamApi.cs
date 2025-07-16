using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services;

public class NhomSanPhamApi : INhomSanPhamApi
{
    private const string BASE_URL = "/api/NhomSanPham";
    string _friendlyName = TuDien._tableFriendlyNames["NhomSanPham".ToLower()];

    public async Task<Result<List<NhomSanPhamDto>>> GetAllAsync()
    {
        var result = await ApiClient.Get<Result<List<NhomSanPhamDto>>>($"{BASE_URL}");
        return result ?? Result<List<NhomSanPhamDto>>.Failure($"Không thể lấy danh sách {_friendlyName}.");
    }

    public async Task<Result<NhomSanPhamDto>> GetByIdAsync(Guid id)
    {
        var result = await ApiClient.Get<Result<NhomSanPhamDto>>($"{BASE_URL}/{id}");
        return result ?? Result<NhomSanPhamDto>.Failure($"Không thể lấy thông tin {_friendlyName}.");
    }

    public async Task<Result<List<NhomSanPhamDto>>> GetUpdatedSince(DateTime since)
    {
        var result = await ApiClient.Get<Result<List<NhomSanPhamDto>>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
        return result ?? Result<List<NhomSanPhamDto>>.Failure($"Không thể lấy danh sách {_friendlyName} đã cập nhật.");
    }

    public async Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}", dto);
        return await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>()
               ?? Result<NhomSanPhamDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        var response = await ApiClient.PutAsync($"{BASE_URL}/{id}", dto);
        return await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>()
               ?? Result<NhomSanPhamDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id)
    {
        var response = await ApiClient.DeleteAsync($"{BASE_URL}/{id}");
        return await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>()
               ?? Result<NhomSanPhamDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<NhomSanPhamDto>> RestoreAsync(Guid id)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}/{id}/restore", (object)null!);
        return await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>()
               ?? Result<NhomSanPhamDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }
}