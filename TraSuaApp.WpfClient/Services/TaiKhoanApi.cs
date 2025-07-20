using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services;

public class TaiKhoanApi : ITaiKhoanApi
{
    private const string BASE_URL = "/api/TaiKhoan";
    string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

    public async Task<Result<List<TaiKhoanDto>>> GetAllAsync()
    {
        var result = await ApiClient.Get<Result<List<TaiKhoanDto>>>($"{BASE_URL}");
        return result ?? Result<List<TaiKhoanDto>>.Failure($"Không thể lấy danh sách {_friendlyName}.");
    }

    public async Task<Result<TaiKhoanDto>> GetByIdAsync(Guid id)
    {
        var result = await ApiClient.Get<Result<TaiKhoanDto>>($"{BASE_URL}/{id}");
        return result ?? Result<TaiKhoanDto>.Failure($"Không thể lấy thông tin {_friendlyName}.");
    }

    public async Task<Result<List<TaiKhoanDto>>> GetUpdatedSince(DateTime since)
    {
        var result = await ApiClient.Get<Result<List<TaiKhoanDto>>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
        return result ?? Result<List<TaiKhoanDto>>.Failure($"Không thể lấy danh sách {_friendlyName} đã cập nhật.");
    }

    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}", dto);
        return await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>()
               ?? Result<TaiKhoanDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        var response = await ApiClient.PutAsync($"{BASE_URL}/{id}", dto);
        return await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>()
               ?? Result<TaiKhoanDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id)
    {
        var response = await ApiClient.DeleteAsync($"{BASE_URL}/{id}");
        return await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>()
               ?? Result<TaiKhoanDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<TaiKhoanDto>> RestoreAsync(Guid id)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}/{id}/restore", (object)null!);
        return await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>()
               ?? Result<TaiKhoanDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }
}