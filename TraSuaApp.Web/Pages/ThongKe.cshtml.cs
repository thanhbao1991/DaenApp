using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ThongKeNgayModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public ThongKeNgayModel(IHttpClientFactory http)
        {
            _http = http;
        }

        [BindProperty(SupportsGet = true)] public int Ngay { get; set; }
        [BindProperty(SupportsGet = true)] public int Thang { get; set; }
        [BindProperty(SupportsGet = true)] public int Nam { get; set; }

        public decimal TongChiTieu { get; set; }
        public List<ChiTieuItemDto> ChiTieuNgay { get; set; } = new();
        public List<ChiTieuItemDto> ChiTieuThang { get; set; } = new();

        public decimal TongCongNo { get; set; }
        public List<CongNoItemDto> CongNo { get; set; } = new();

        public decimal TongThanhToan { get; set; }
        public decimal ChuyenKhoan { get; set; }
        public List<ThanhToanItemDto> ThanhToan { get; set; } = new();

        public decimal TongDoanhThu { get; set; }
        public List<DoanhThuTheoLoaiItemDto> DoanhThu { get; set; } = new();

        public decimal TongTraNo { get; set; }
        public List<KhachTraNoItemDto> TraNoTaiQuan { get; set; } = new();
        public List<KhachTraNoItemDto> TraNoShipper { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (Ngay == 0)
            {
                var now = DateTime.Today;
                Ngay = now.Day;
                Thang = now.Month;
                Nam = now.Year;
            }

            var client = _http.CreateClient("Api");

            async Task<T?> Get<T>(string url)
            {
                var res = await client.GetAsync(url);

                if (res.StatusCode == HttpStatusCode.Unauthorized)
                    return default;

                var json = await res.Content.ReadAsStringAsync();

                var wrapper = JsonSerializer.Deserialize<Result<T>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return wrapper.Data;
            }

            var qs = $"?ngay={Ngay}&thang={Thang}&nam={Nam}";

            var chiTieu = await Get<ThongKeChiTieuDto>($"api/ThongKe/chi-tieu-ngay{qs}");
            if (chiTieu != null)
            {
                TongChiTieu = chiTieu.TongChiTieu;
                ChiTieuNgay = chiTieu.DanhSachChiTieuNgay;
                ChiTieuThang = chiTieu.DanhSachChiTieuThang;
            }

            var congNo = await Get<ThongKeCongNoDto>($"api/ThongKe/cong-no-ngay{qs}");
            if (congNo != null)
            {
                TongCongNo = congNo.TongCongNoNgay;
                CongNo = congNo.DanhSachCongNoNgay;
            }

            var thanhToan = await Get<ThongKeThanhToanDto>($"api/ThongKe/thanh-toan-ngay{qs}");
            if (thanhToan != null)
            {
                ThanhToan = thanhToan.DanhSachTienMat;
                ChuyenKhoan = thanhToan.TongChuyenKhoan;
                TongThanhToan = thanhToan.TongTienMat + thanhToan.TongChuyenKhoan;
            }

            var doanhThu = await Get<ThongKeDoanhThuNgayDto>($"api/ThongKe/doanh-thu-ngay{qs}");
            if (doanhThu != null)
            {
                TongDoanhThu = doanhThu.TongDoanhThu;
                DoanhThu = doanhThu.DanhSach;
            }

            var traNo = await Get<ThongKeTraNoNgayDto>($"api/ThongKe/tra-no-ngay{qs}");
            if (traNo != null)
            {
                TongTraNo = traNo.TongTraNoTaiQuan + traNo.TongTraNoShipper;
                TraNoTaiQuan = traNo.TraNoTaiQuan;
                TraNoShipper = traNo.TraNoShipper;
            }

            var chuaThanhToan = await Get<ThongKeDonChuaThanhToanDto>($"api/ThongKe/don-chua-thanh-toan{qs}");
            if (chuaThanhToan != null)
            {
                TongChuaThanhToan = chuaThanhToan.TongChuaThanhToan;
                ChuaThanhToan = chuaThanhToan.DanhSach;
            }
            return Page();
        }
        public decimal TongChuaThanhToan { get; set; }
        public List<DonChuaThanhToanItemDto> ChuaThanhToan { get; set; } = new();
    }
}