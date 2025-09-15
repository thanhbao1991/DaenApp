using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class DoanhThuThangModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DoanhThuThangModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<DoanhThuThangItemDto> DoanhThuTheoNgay { get; set; } = new();
        public decimal TongDoanhThu { get; set; }
        public decimal TongChiTieu { get; set; }
        public decimal TongSoDon { get; set; }
        public decimal TongChuyenKhoan { get; set; }
        public decimal TongTienMat { get; set; }
        public decimal TongTienNo { get; set; }

        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public async Task OnGetAsync()
        {
            if (Thang == 0 || Nam == 0)
            {
                var today = DateTime.Today;
                Thang = today.Month;
                Nam = today.Year;
            }

            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/doanhthu/thang?thang={Thang}&nam={Nam}");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<Result<List<DoanhThuThangItemDto>>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                DoanhThuTheoNgay = wrapper?.Data ?? new();

                // Dùng dữ liệu từ backend đã tính sẵn
                TongDoanhThu = DoanhThuTheoNgay.Sum(d => d.TongTien);
                TongChiTieu = DoanhThuTheoNgay.Sum(d => d.ChiTieu);
                TongSoDon = DoanhThuTheoNgay.Sum(d => d.SoDon);
                TongChuyenKhoan = DoanhThuTheoNgay.Sum(d => d.TienBank);
                TongTienNo = DoanhThuTheoNgay.Sum(d => d.TienNo);
                TongTienMat = DoanhThuTheoNgay.Sum(d => d.TongTienMat);
            }
        }
    }
}