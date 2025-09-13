using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class DoanhThuNgayModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DoanhThuNgayModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public DoanhThuNgayDto? Data { get; set; }

        [BindProperty(SupportsGet = true)] public int Ngay { get; set; }
        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public async Task OnGetAsync()
        {
            if (Thang == 0 || Nam == 0 || Ngay == 0)
            {
                var today = DateTime.Today;
                Ngay = today.Day;
                Thang = today.Month;
                Nam = today.Year;
            }

            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/doanhthu/ngay?ngay={Ngay}&thang={Thang}&nam={Nam}");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<Result<DoanhThuNgayDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Data = wrapper?.Data;
            }
        }
    }
}