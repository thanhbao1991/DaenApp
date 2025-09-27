using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ChiTieuNhaModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTieuNhaModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<ChiTieuHangNgayDto> Items { get; set; } = new();
        public decimal Tong { get; set; }

        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public DateTime Prev { get; set; }
        public DateTime Next { get; set; }
        public DateTime CurrentMonthStart { get; set; } = DateTime.Today;

        public async Task OnGetAsync()
        {
            if (Thang == 0 || Nam == 0)
            {
                var today = DateTime.Today;
                Thang = today.Month;
                Nam = today.Year;
            }

            CurrentMonthStart = new DateTime(Nam, Thang, 1);
            Prev = CurrentMonthStart.AddMonths(-1);
            Next = CurrentMonthStart.AddMonths(1);

            var client = _httpClientFactory.CreateClient("Api");
            // endpoint đã có: GET api/ChiTieuHangNgay/nguyenlieu/{year}/{month}
            var res = await client.GetAsync($"api/ChiTieuHangNgay/nguyenlieu/{Nam}/{Thang}");
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<Result<List<ChiTieuHangNgayDto>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var data = wrapper?.Data ?? new List<ChiTieuHangNgayDto>();
            Items = data.OrderByDescending(x => x.Ngay).ToList(); // mới nhất lên đầu
            Tong = Items.Sum(x => x.ThanhTien);
        }
    }
}