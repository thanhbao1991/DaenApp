using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TraSuaApp.WpfClient.Helpers
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5093") // ✅ Đổi lại theo cấu hình thực tế nếu cần
        };

        public static event Action? OnTokenExpired;

        //public static HttpClient Instance
        //{
        //    get
        //    {
        //        AddAuthorizationHeader();
        //        return _httpClient;
        //    }
        //}

        static ApiClient()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static void AddAuthorizationHeader()
        {
            var token = Properties.Settings.Default.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                if (_httpClient.DefaultRequestHeaders.Authorization == null ||
                    _httpClient.DefaultRequestHeaders.Authorization.Parameter != token)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }

        public static void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Properties.Settings.Default.Token = token;
            Properties.Settings.Default.Save();
        }

        // ✅ Hàm xử lý chung để phát hiện lỗi token
        private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> apiCall)
        {
            try
            {
                var response = await apiCall();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    OnTokenExpired?.Invoke();
                }

                return response;
            }
            catch
            {
                throw; // để nơi gọi tự xử lý
            }
        }

        // ✅ Các phương thức API
        public static async Task<T?> Get<T>(string uri, bool includeToken = true)
        {
            var response = await GetAsync(uri, includeToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }

            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"{msg}");
        }

        public static Task<HttpResponseMessage> GetAsync(string uri, bool includeToken = true)
        {
            if (includeToken) AddAuthorizationHeader();
            return SendAsync(() => _httpClient.GetAsync(uri));
        }

        public static Task<HttpResponseMessage> PostAsync<T>(string uri, T content, bool includeToken = true)
        {
            if (includeToken) AddAuthorizationHeader();
            return SendAsync(() => _httpClient.PostAsJsonAsync(uri, content));
        }

        public static Task<HttpResponseMessage> PutAsync<T>(string uri, T content, bool includeToken = true)
        {
            if (includeToken) AddAuthorizationHeader();
            return SendAsync(() => _httpClient.PutAsJsonAsync(uri, content));
        }

        public static Task<HttpResponseMessage> DeleteAsync(string uri, bool includeToken = true)
        {
            if (includeToken) AddAuthorizationHeader();
            return SendAsync(() => _httpClient.DeleteAsync(uri));
        }
    }
}