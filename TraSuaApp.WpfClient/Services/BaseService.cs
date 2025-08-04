using System.Net.Http;
using System.Net.Http.Json;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Apis
{
    public abstract class BaseApi
    {
        protected readonly string _friendlyName;

        protected BaseApi(string friendlyName)
        {
            _friendlyName = friendlyName;
        }

        /// <summary>
        /// Xử lý phản hồi chung cho tất cả API
        /// </summary>
        protected async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return Result<T>.Failure(
                        $"Lỗi 400 - Yêu cầu không hợp lệ.\nChi tiết: {errorContent}"
                    );
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Result<T>.Failure("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                }

                return Result<T>.Failure(
                    $"API trả về lỗi {(int)response.StatusCode} - {response.ReasonPhrase}\nChi tiết: {errorContent}"
                );
            }

            return await response.Content.ReadFromJsonAsync<Result<T>>()
                   ?? Result<T>.Failure($"Không đọc được kết quả {_friendlyName} từ API.");
        }

        // ==== Các hàm CRUD chung ====

        protected async Task<Result<T>> GetAsync<T>(string url)
        {
            var response = await ApiClient.GetAsync(url);
            return await HandleResponseAsync<T>(response);
        }

        protected async Task<Result<T>> PostAsync<T>(string url, object? dto)
        {
            var response = await ApiClient.PostAsync(url, dto);
            return await HandleResponseAsync<T>(response);
        }

        protected async Task<Result<T>> PutAsync<T>(string url, object? dto)
        {
            var response = await ApiClient.PutAsync(url, dto);
            return await HandleResponseAsync<T>(response);
        }

        protected async Task<Result<T>> DeleteAsync<T>(string url)
        {
            var response = await ApiClient.DeleteAsync(url);
            return await HandleResponseAsync<T>(response);
        }
    }
}
