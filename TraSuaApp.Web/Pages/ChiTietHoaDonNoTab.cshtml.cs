using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    [IgnoreAntiforgeryToken]
    public class ChiTietHoaDonNoTabModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public List<HoaDonNoDto> Items { get; set; } = new();

        public ChiTietHoaDonNoTabModel(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("Api");

            var res = await client.GetFromJsonAsync<Result<List<HoaDonNoDto>>>(
                "/api/dashboard/get-cong-no"
            );

            Items = res?.IsSuccess == true && res.Data != null
                ? res.Data
                : new();
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

        public async Task<IActionResult> OnPostPayAsync([FromBody] PayRequestDto req)
        {
            var api = _http.CreateClient("Api");

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
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var json = await res.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }
}