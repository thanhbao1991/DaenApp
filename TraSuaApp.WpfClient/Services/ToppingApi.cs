using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services;

public class ToppingApi : IToppingApi
{
    private const string BASE_URL = "/api/Topping";
    string _friendlyName = TuDien._tableFriendlyNames["Topping"];

    public async Task<Result<List<ToppingDto>>> GetAllAsync()
    {
        var result = await ApiClient.Get<Result<List<ToppingDto>>>($"{BASE_URL}");
        return result ?? Result<List<ToppingDto>>.Failure($"Không thể lấy danh sách {_friendlyName}.");
    }

    public async Task<Result<ToppingDto>> GetByIdAsync(Guid id)
    {
        var result = await ApiClient.Get<Result<ToppingDto>>($"{BASE_URL}/{id}");
        return result ?? Result<ToppingDto>.Failure($"Không thể lấy thông tin {_friendlyName}.");
    }

    public async Task<Result<List<ToppingDto>>> GetUpdatedSince(DateTime since)
    {
        var result = await ApiClient.Get<Result<List<ToppingDto>>>($"{BASE_URL}/updated-since/{since:yyyy-MM-ddTHH:mm:ss}");
        return result ?? Result<List<ToppingDto>>.Failure($"Không thể lấy danh sách {_friendlyName} đã cập nhật.");
    }

    public async Task<Result<ToppingDto>> CreateAsync(ToppingDto dto)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}", dto);
        return await response.Content.ReadFromJsonAsync<Result<ToppingDto>>()
               ?? Result<ToppingDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto)
    {
        var response = await ApiClient.PutAsync($"{BASE_URL}/{id}", dto);
        return await response.Content.ReadFromJsonAsync<Result<ToppingDto>>()
               ?? Result<ToppingDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<ToppingDto>> DeleteAsync(Guid id)
    {
        var response = await ApiClient.DeleteAsync($"{BASE_URL}/{id}");
        return await response.Content.ReadFromJsonAsync<Result<ToppingDto>>()
               ?? Result<ToppingDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }

    public async Task<Result<ToppingDto>> RestoreAsync(Guid id)
    {
        var response = await ApiClient.PostAsync($"{BASE_URL}/{id}/restore", (object)null!);
        return await response.Content.ReadFromJsonAsync<Result<ToppingDto>>()
               ?? Result<ToppingDto>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
    }
}