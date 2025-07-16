using System.Net.Http.Json;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services;

public class KhachHangApi : IKhachHangApi
{
    private const string BASE_URL = "/api/khachhang";
    string _friendlyName = TuDien._tableFriendlyNames["NhomSanPham".ToLower()];

    public async Task<Result<List<KhachHangDto>>> GetAllAsync()
    {
        var result = await ApiClient.Get<Result<List<KhachHangDto>>>($"{BASE_URL}");
        return result ?? Result<List<KhachHangDto>>.Failure("Không thể lấy danh sách {_friendlyName}.");
    }

    public async Task<Result<KhachHangDto>> GetByIdAsync(Guid id)
    {
        var result = await ApiClient.Get<Result<KhachHangDto>>($"{BASE_URL}/{id}");
        return result ?? Result<KhachHangDto>.Failure($"Không thể lấy thông tin {_friendlyName}.");
    }
    public async Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto)
    {
        var response = await ApiClient.PostAsync(BASE_URL, dto);
        return await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>()
               ?? Result<KhachHangDto>.Failure("Không đọc được kết quả từ API.");
    }

    public async Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto)
    {
        var response = await ApiClient.PutAsync($"{BASE_URL}/{id}", dto);
        return await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>()
               ?? Result<KhachHangDto>.Failure("Không đọc được kết quả từ API.");
    }

    public async Task<Result<KhachHangDto>> DeleteAsync(Guid id)
    {
        var response = await ApiClient.DeleteAsync($"{BASE_URL}/{id}");
        return await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>()
               ?? Result<KhachHangDto>.Failure("Không đọc được kết quả từ API.");
    }

    public async Task<Result<KhachHangDto>> RestoreAsync(Guid id)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}/{id}/restore", (object)null!);
        return await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>()
               ?? Result<KhachHangDto>.Failure("Không đọc được kết quả từ API.");
    }
    public async Task<Result<List<KhachHangDto>>> GetUpdatedSince(DateTime since)
    {
        var result = await ApiClient.Get<Result<List<KhachHangDto>>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
        return result ?? Result<List<KhachHangDto>>.Failure("Không thể lấy danh sách đã cập nhật.");
    }
}