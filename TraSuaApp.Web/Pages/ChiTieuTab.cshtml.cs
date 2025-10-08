using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ChiTieuTabModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTieuTabModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<ChiTieuHangNgayDto> Items { get; set; } = new();
        public decimal Tong { get; set; }

        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }
        [BindProperty(SupportsGet = true)] public Guid NguyenLieuId { get; set; } = Guid.Empty;

        private static readonly Guid DefaultNguyenLieuId = Guid.Parse("7995B334-44D1-4768-89C7-280E6B0413AE");

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

            if (NguyenLieuId == Guid.Empty)
                NguyenLieuId = DefaultNguyenLieuId;

            CurrentMonthStart = new DateTime(Nam, Thang, 1);
            Prev = CurrentMonthStart.AddMonths(-1);
            Next = CurrentMonthStart.AddMonths(1);

            var client = _httpClientFactory.CreateClient("Api");

            // ✅ Route chuẩn khớp backend
            var offset = GetOffset(Nam, Thang);
            var res = await client.GetAsync($"api/dashboard/chitieubynguyenlieuid?offset={offset}&nguyenLieuId={NguyenLieuId}");
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<Result<List<ChiTieuHangNgayDto>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var data = wrapper?.Data ?? new List<ChiTieuHangNgayDto>();
            Items = data.OrderByDescending(x => x.Ngay).ToList();
            Tong = Items.Sum(x => x.ThanhTien);
        }

        private static int GetOffset(int year, int month)
        {
            var now = DateTime.Today;
            var baseMonth = new DateTime(now.Year, now.Month, 1);
            var target = new DateTime(year, month, 1);
            return (target.Year - baseMonth.Year) * 12 + (target.Month - baseMonth.Month);
        }
    }
}