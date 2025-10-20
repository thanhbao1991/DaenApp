using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class XepHangSanPhamModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public XepHangSanPhamModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<SanPhamXepHangDto> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int Year { get; set; }
        public int PrevYear => Year <= 0 ? DateTime.Today.Year - 1 : Year - 1;
        public int NextYear => Year <= 0 ? DateTime.Today.Year + 1 : Year + 1;
        public int CurrentYear => Year <= 0 ? DateTime.Today.Year : Year;

        public async Task OnGetAsync()
        {
            if (Year <= 0) Year = DateTime.Today.Year;

            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/dashboard/xephang-sanpham?year={Year}");
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<Result<List<SanPhamXepHangDto>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Items = wrapper?.Data ?? new();
        }
    }
}