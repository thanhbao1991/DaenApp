using System.Net.Http.Headers;

namespace TraSuaAppWeb.Handlers
{
    // Handler gắn Authorization: Bearer <token> từ cookie vào mọi call HttpClient("Api")
    public sealed class ApiAuthHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _cfg;

        public ApiAuthHeaderHandler(IHttpContextAccessor http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = _http.HttpContext?.Request.Cookies["access_token"];
            if (string.IsNullOrWhiteSpace(token))
                token = _cfg["ApiSettings:StaticBearer"]; // tuỳ chọn fallback

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, ct);
        }
    }
}