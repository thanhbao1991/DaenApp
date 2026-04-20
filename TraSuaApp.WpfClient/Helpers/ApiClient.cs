using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.WpfClient.Helpers
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };

        // 🟟 cho phép set lại
        private static string[] _servers = Array.Empty<string>();

        private static string _currentBaseUrl = "";
        private static bool _baseUrlChecked;

        public static string? ConnectionId { get; set; }
        public static event Action? OnTokenExpired;

        static ApiClient()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // 🟟 detect username Windows
            var username = Environment.UserName.ToLower();

            if (username == "admin")
            {
                // 🟟 ưu tiên internet
                _servers = new[]
                {
                    "http://www.denncoffee.uk",
                    "http://192.168.1.12"
                };

                _currentBaseUrl = _servers[0];
            }
            else if (username == "ty")
            {
                // 🟟 ưu tiên LAN
                _servers = new[]
                {
                    "http://192.168.1.12",
                    "http://www.denncoffee.uk"
                };

                _currentBaseUrl = _servers[0];
            }
            else
            {
                // default
                _servers = new[]
                {
                    "http://192.168.1.12",
                    "http://www.denncoffee.uk"
                };

                _currentBaseUrl = _servers[0];
            }

            Console.WriteLine($"[ApiClient] User: {username} - BaseUrl: {_currentBaseUrl}");
        }

        public static void SetBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            _currentBaseUrl = NormalizeUrl(baseUrl);
            _baseUrlChecked = true;
        }

        public static async Task<string> DetectBestServerAsync()
        {
            if (_baseUrlChecked && await IsAliveAsync(_currentBaseUrl))
                return _currentBaseUrl;

            var tasks = _servers.Select(async s => new
            {
                Url = s,
                Alive = await IsAliveAsync(s)
            });

            var results = await Task.WhenAll(tasks);

            var aliveServer = results.FirstOrDefault(x => x.Alive);

            if (aliveServer != null)
            {
                SetBaseUrl(aliveServer.Url);
                return aliveServer.Url;
            }

            SetBaseUrl(_servers[0]);
            return _servers[0];
        }

        private static string NormalizeUrl(string url)
        {
            url = url.Trim().TrimEnd('/');

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            return url.TrimEnd('/');
        }

        private static async Task<bool> IsAliveAsync(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static void AddAuthorizationHeader()
        {
            var token = Properties.Settings.Default.Token;

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (_httpClient.DefaultRequestHeaders.Authorization == null ||
                    _httpClient.DefaultRequestHeaders.Authorization.Parameter != token)
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
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
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            Properties.Settings.Default.Token = token;
            Properties.Settings.Default.Save();
        }

        private static HttpRequestMessage BuildRequest(Func<HttpRequestMessage> createRequest)
        {
            var request = createRequest();

            if (!request.RequestUri!.IsAbsoluteUri)
            {
                var fullUrl = _currentBaseUrl.TrimEnd('/') + "/" +
                              request.RequestUri.ToString().TrimStart('/');

                request.RequestUri = new Uri(fullUrl);
            }

            return request;
        }

        private static async Task<HttpResponseMessage> SendAsync(
            Func<HttpRequestMessage> createRequest,
            bool includeToken,
            CancellationToken ct = default)
        {
            if (!_baseUrlChecked)
            {
                await DetectBestServerAsync();
            }

            var request = BuildRequest(createRequest);

            if (includeToken)
                AddAuthorizationHeader();

            AddConnectionIdHeader(request);

            try
            {
                var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    OnTokenExpired?.Invoke();
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 🟟 retry sang server khác
                foreach (var server in _servers)
                {
                    if (server == _currentBaseUrl)
                        continue;

                    if (await IsAliveAsync(server))
                    {
                        _currentBaseUrl = server;

                        var retryRequest = BuildRequest(createRequest);

                        if (includeToken)
                            AddAuthorizationHeader();

                        AddConnectionIdHeader(retryRequest);

                        var retryResponse = await _httpClient.SendAsync(
                            retryRequest,
                            HttpCompletionOption.ResponseHeadersRead,
                            ct);

                        if (retryResponse.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            OnTokenExpired?.Invoke();
                        }

                        return retryResponse;
                    }
                }

                await DiscordService.SendAsync(DiscordEventType.Admin, ex.ToString());

                throw new Exception("Không thể kết nối server: " + ex.Message);
            }
        }

        public static async Task<T?> Get<T>(
            string uri,
            bool includeToken = true,
            CancellationToken ct = default)
        {
            var response = await GetAsync(uri, includeToken, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            }

            var msg = await response.Content.ReadAsStringAsync(ct);
            NotiHelper.ShowError(msg);
            throw new Exception(msg);
        }

        public static Task<HttpResponseMessage> GetAsync(
            string uri,
            bool includeToken = true,
            CancellationToken ct = default)
        {
            return SendAsync(() => new HttpRequestMessage(HttpMethod.Get, uri), includeToken, ct);
        }

        public static Task<HttpResponseMessage> PostAsync<T>(
            string uri,
            T content,
            bool includeToken = true,
            CancellationToken ct = default)
        {
            return SendAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = JsonContent.Create(content)
                };
                return request;
            }, includeToken, ct);
        }

        public static Task<HttpResponseMessage> PutAsync<T>(
            string uri,
            T content,
            bool includeToken = true,
            CancellationToken ct = default)
        {
            return SendAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, uri)
                {
                    Content = JsonContent.Create(content)
                };
                return request;
            }, includeToken, ct);
        }

        public static Task<HttpResponseMessage> DeleteAsync(
            string uri,
            bool includeToken = true,
            CancellationToken ct = default)
        {
            return SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, uri), includeToken, ct);
        }
    }
}