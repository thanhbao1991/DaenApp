using System.Net;
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
        public decimal ThuongNha { get; set; }
        public decimal ThuongKhanh { get; set; }

        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Thang == 0 || Nam == 0)
            {
                var today = DateTime.Today;
                Thang = today.Month;
                Nam = today.Year;
            }

            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/doanhthu/thang?thang={Thang}&nam={Nam}");

            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var returnUrl = $"{Request.Path}{Request.QueryString}";
                return Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

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
                ThuongNha = DoanhThuTheoNgay.Sum(d => d.ThuongNha);
                ThuongKhanh = DoanhThuTheoNgay.Sum(d => d.ThuongKhanh);
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