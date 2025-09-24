using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ThongKeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ThongKeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ThongKeNgayDto? Data { get; set; }

        [BindProperty(SupportsGet = true)] public int Ngay { get; set; }
        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public DateTime Prev { get; set; }
        public DateTime Next { get; set; }

        public async Task OnGetAsync()
        {
            if (Ngay == 0 || Thang == 0 || Nam == 0)
            {
                var today = DateTime.Today;
                Ngay = today.Day;
                Thang = today.Month;
                Nam = today.Year;
            }

            var currentDate = new DateTime(Nam, Thang, Ngay);
            Prev = currentDate.AddDays(-1);
            Next = currentDate.AddDays(1);

            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/thongke/ngay?ngay={Ngay}&thang={Thang}&nam={Nam}");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<Result<ThongKeNgayDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Data = wrapper?.Data;
            }
        }
    }
}