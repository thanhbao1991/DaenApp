using System.Net;
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
            var res = await client.GetAsync($"api/doanhthu/ngay?ngay={Ngay}&thang={Thang}&nam={Nam}");

            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var returnUrl = $"{Request.Path}{Request.QueryString}";
                return Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<Result<DoanhThuNgayDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Data = wrapper?.Data;
            }

            return Page();
        }

        // ========= PROXY CHO AJAX: Chi tiết hoá đơn =========
        public async Task<IActionResult> OnGetChiTiet(Guid hoaDonId)
        {
            var client = _httpClientFactory.CreateClient("Api");
            var upstream = await client.GetAsync($"api/doanhthu/chitiet?hoaDonId={hoaDonId}");
            var body = await upstream.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = body,
                ContentType = "application/json",
                StatusCode = (int)upstream.StatusCode
            };
        }

        // ========= PROXY CHO AJAX: Danh sách hoá đơn theo khách =========
        public async Task<IActionResult> OnGetDanhSach(Guid khachHangId)
        {
            var client = _httpClientFactory.CreateClient("Api");
            var upstream = await client.GetAsync($"api/doanhthu/danhsach?khachHangId={khachHangId}");
            var body = await upstream.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = body,
                ContentType = "application/json",
                StatusCode = (int)upstream.StatusCode
            };
        }
    }
}