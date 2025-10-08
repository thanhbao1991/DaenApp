using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class VoucherTabModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public VoucherTabModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<VoucherChiTraDto> Items { get; set; } = new();
        public decimal Tong { get; set; }

        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }
        [BindProperty(SupportsGet = true)] public Guid VoucherId { get; set; } = Guid.Empty;

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
            var offset = GetOffset(Nam, Thang);

            // ✅ Truyền voucherId khi gọi API
            var url = $"api/dashboard/voucher?offset={offset}&voucherId={VoucherId}";
            var res = await client.GetAsync(url);
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<Result<List<VoucherChiTraDto>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var data = wrapper?.Data ?? new List<VoucherChiTraDto>();
            Items = data.OrderByDescending(x => x.Ngay).ToList();
            Tong = Items.Sum(x => x.GiaTriApDung);
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