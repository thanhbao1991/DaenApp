using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    [IgnoreAntiforgeryToken]
    public class DoanhThuNgayModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DoanhThuNgayModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<HoaDonNoDto> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int Ngay { get; set; }
        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Thang == 0 || Nam == 0 || Ngay == 0)
            {
                var today = DateTime.Today;
                Ngay = today.Day;
                Thang = today.Month;
                Nam = today.Year;
            }

            var client = _httpClientFactory.CreateClient("Api");

            var res = await client.GetAsync("/api/dashboard/get-hoa-don");

            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var returnUrl = $"{Request.Path}{Request.QueryString}";
                return Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();

                var wrapper = JsonSerializer.Deserialize<Result<List<HoaDonNoDto>>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                Items = wrapper?.Data ?? new();
            }

            return Page();
        }

        public class PayRequestDto
        {
            public Guid Id { get; set; }
            public Guid? KhachHangId { get; set; }
            public string? Ten { get; set; }
            public decimal SoTien { get; set; }
            public string? GhiChu { get; set; }
            public Guid PhuongThucThanhToanId { get; set; }
        }

        public class F12RequestDto
        {
            public Guid Id { get; set; }
            public JsonElement Dto { get; set; }
        }

        public async Task<IActionResult> OnPostPayAsync([FromBody] PayRequestDto req)
        {
            var api = _httpClientFactory.CreateClient("Api");

            var payload = new
            {
                req.KhachHangId,
                req.Ten,
                req.SoTien,
                req.GhiChu,
                req.PhuongThucThanhToanId
            };

            var res = await api.PutAsync(
                $"/api/HoaDon/{req.Id}/f1f4",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var json = await res.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        public async Task<IActionResult> OnPostF12Async([FromBody] F12RequestDto req)
        {
            var api = _httpClientFactory.CreateClient("Api");

            var res = await api.PutAsync(
                $"/api/HoaDon/{req.Id}/f12",
                new StringContent(
                    req.Dto.GetRawText(),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var json = await res.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }
}