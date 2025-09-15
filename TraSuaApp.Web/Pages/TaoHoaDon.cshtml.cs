#nullable disable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Hubs;
using TraSuaAppWeb.Models;

namespace TraSuaAppWeb.Pages
{
    public class KiemTraGiaRequest
    {
        public int IdKhachHang { get; set; }
        public List<SanPhamKiemTra> SanPhams { get; set; }
    }

    public class SanPhamKiemTra
    {
        public int IdSanPham { get; set; }
        public int DonGia { get; set; }
    }

    public class GiaKhacModel
    {
        public string TenSanPham { get; set; }
        public int gia_cu { get; set; }
        public int gia_moi { get; set; }
    }
    public class TaoHoaDonModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<HoaDonHub> _hubContext;

        public TaoHoaDonModel(AppDbContext context
        , IHubContext<HoaDonHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        [BindProperty] public required HoaDon HoaDon { get; set; }
        [BindProperty] public required List<ChiTietHoaDon> ChiTietHoaDonList { get; set; }

        public required List<KhachHang> KhachHangList { get; set; }
        public required List<SanPham> SanPhamList { get; set; }

        public void OnGet()
        {
            KhachHangList = _context.KhachHang.ToList();
            SanPhamList = _context.SanPham
                .Where(p => !p.NgungBan && p.ChiTieu == false).ToList();
        }
        public async Task<IActionResult> OnPostKiemTraGiaAsync([FromBody] KiemTraGiaRequest request)
        {
            try
            {
                var ketQua = new List<GiaKhacModel>();

                foreach (var item in request.SanPhams)
                {
                    var giaGanNhat = await _context.ChiTietHoaDon
                        .Include(ct => ct.HoaDon) // Cần Include để truy cập được IdKhachHang
                        .Where(ct => ct.HoaDon.IdKhachHang == request.IdKhachHang && ct.IdSanPham == item.IdSanPham)
                        .OrderByDescending(ct => ct.HoaDon.NgayHoaDon)
                        .Select(ct => ct.DonGia)
                        .FirstOrDefaultAsync();

                    if (giaGanNhat > 0 && giaGanNhat != item.DonGia)
                    {
                        var tenSP = await _context.SanPham
                            .Where(sp => sp.IdSanPham == item.IdSanPham)
                            .Select(sp => sp.TenSanPham)
                            .FirstOrDefaultAsync();

                        ketQua.Add(new GiaKhacModel
                        {
                            TenSanPham = tenSP,
                            gia_cu = (int)giaGanNhat,
                            gia_moi = item.DonGia
                        });
                    }
                }

                return new JsonResult(ketQua);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogAsync(ex);
                return new JsonResult(new { success = false });
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            HoaDon.NgayHoaDon = DateTime.Now;
            HoaDon.IdBan = 1045;
            HoaDon.TongTien = 0;
            _context.HoaDon.Add(HoaDon);
            await _context.SaveChangesAsync();

            foreach (var ct in ChiTietHoaDonList ?? Enumerable.Empty<ChiTietHoaDon>())
            {
                if (ct.IdSanPham <= 0 || ct.SoLuong <= 0) continue;
                ct.IdHoaDon = HoaDon.IdHoaDon;
                var sp = await _context.SanPham.FindAsync(ct.IdSanPham);
                if (sp != null)
                {
                    ct.TichDiem = sp.TichDiem;
                    ct.TenSanPham = sp.TenSanPham;
                    if (ct.DonGia >= -100 && ct.DonGia < 0)
                        ct.ThanhTien = -(ct.DonGia / 100 * HoaDon.TongTien);
                    else
                        ct.ThanhTien = ct.DonGia * ct.SoLuong;
                }
                HoaDon.TongTien += ct.ThanhTien;
                _context.ChiTietHoaDon.Add(ct);
            }

            HoaDon.ConLai = HoaDon.TongTien;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("HoaDonMoi", new
            {
                id = HoaDon.IdHoaDon,
                ten_khach = HoaDon.ThongTinHoaDon,
                tong_tien = HoaDon.TongTien
            });

            return RedirectToPage("DoanhThuNgay");
        }
    }
}