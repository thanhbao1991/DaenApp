using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaAppWeb.Pages
{
    [IgnoreAntiforgeryToken]
    public class ChiTietThangModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public List<ChiTietThangDto> Items { get; set; } = new();

        public int Thang { get; set; }
        public int Nam { get; set; }

        public ChiTietThangModel(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task OnGetAsync(int thang = 0, int nam = 0)
        {
            var now = DateTime.Now;

            Thang = thang <= 0 ? now.Month : thang;
            Nam = nam <= 0 ? now.Year : nam;

            if (Thang < 1 || Thang > 12)
            {
                Thang = now.Month;
                Nam = now.Year;
            }

            var client = _http.CreateClient("Api");

            var res = await client.GetFromJsonAsync<Result<List<ChiTietThangDto>>>(
                $"/api/dashboard/get-chi-tiet-thang?thang={Thang}&nam={Nam}"
            );

            Items = res?.IsSuccess == true && res.Data != null
                ? res.Data
                : new();
        }
    }
}