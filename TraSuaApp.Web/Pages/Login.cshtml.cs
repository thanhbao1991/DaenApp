using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public LoginModel(IHttpClientFactory http) => _http = http;

        [BindProperty] public string TaiKhoan { get; set; } = "";
        [BindProperty] public string MatKhau { get; set; } = "";
        [BindProperty] public bool Remember { get; set; } = true;

        // Bind returnUrl từ query hoặc hidden input trong form
        [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

        public string? Error { get; set; }

        public void OnGet()
        {
            // Nếu đã có token thì quay về trang mong muốn (nếu local), hoặc Index
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                    Response.Redirect(ReturnUrl);
                else
                    Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(MatKhau))
            {
                Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return Page();
            }

            var client = _http.CreateClient("Api");
            var payload = JsonSerializer.Serialize(new LoginRequest { TaiKhoan = TaiKhoan.Trim(), MatKhau = MatKhau });
            var res = await client.PostAsync("api/auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!res.IsSuccessStatusCode)
            {
                Error = "Đăng nhập thất bại.";
                return Page();
            }

            var body = await res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Result<LoginResponse>>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.IsSuccess != true || result.Data == null || string.IsNullOrWhiteSpace(result.Data.Token))
            {
                Error = result?.Message ?? "Đăng nhập thất bại.";
                return Page();
            }

            // Lưu cookie token + tên hiển thị
            var expires = Remember ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8);
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // bật true nếu chạy HTTPS thật
                SameSite = SameSiteMode.Lax,
                Expires = expires
            };
            Response.Cookies.Append("access_token", result.Data.Token, opts);
            Response.Cookies.Append("display_name", result.Data.TenHienThi ?? "", new CookieOptions { Expires = expires });

            // Điều hướng về returnUrl nếu là URL local, ngược lại về Index
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return RedirectToPage("/Index");
        }
    }
}