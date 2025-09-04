using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.WpfClient.Helpers
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Config.ApiBaseUrl)
        };

        public static string? ConnectionId { get; set; }
        public static Uri BaseAddress => _httpClient.BaseAddress!;
        public static event Action? OnTokenExpired;

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

        private static void AddConnectionIdHeader(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(ConnectionId))
            {
                if (request.Headers.Contains("X-Connection-Id"))
                    request.Headers.Remove("X-Connection-Id");

                request.Headers.Add("X-Connection-Id", ConnectionId);
            }
        }

        public static void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Properties.Settings.Default.Token = token;
            Properties.Settings.Default.Save();
        }

        private static async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest, bool includeToken)
        {
            try
            {
                var request = createRequest();

                if (includeToken) AddAuthorizationHeader();
                AddConnectionIdHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    OnTokenExpired?.Invoke();
                }

                return response;
            }
            catch
            {
                throw;
            }
        }

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
            return SendAsync(() => new HttpRequestMessage(HttpMethod.Get, uri), includeToken);
        }

        public static Task<HttpResponseMessage> PostAsync<T>(string uri, T content, bool includeToken = true)
        {
            return SendAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = JsonContent.Create(content)
                };
                return request;
            }, includeToken);
        }

        public static Task<HttpResponseMessage> PutAsync<T>(string uri, T content, bool includeToken = true)
        {
            return SendAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, uri)
                {
                    Content = JsonContent.Create(content)
                };
                return request;
            }, includeToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(string uri, bool includeToken = true)
        {
            return SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, uri), includeToken);
        }


    }
}
