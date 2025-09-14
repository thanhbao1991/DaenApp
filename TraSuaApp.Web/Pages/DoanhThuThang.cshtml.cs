using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Data;


namespace TraSuaAppWeb.Pages
{
    public class DoanhThuThangModel : PageModel
    {
        private readonly AppDbContext _context;
        public DoanhThuThangModel(AppDbContext context)
        {
            _context = context;
        }
        public List<DoanhThuItem> DoanhThuTheoNgay { get; set; } = new();
        public double TongDoanhThu { get; set; }
        public double TongChiTieu { get; set; }
        public double TongSoDon { get; set; }
        public double TongChuyenKhoan { get; set; }
        public double TongTienMat { get; set; }
        public double TongTienNo { get; set; }
        [BindProperty(SupportsGet = true)]
        public int Thang { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Nam { get; set; }
        public async Task OnGetAsync()
        {
            if (Thang == 0 || Nam == 0)
            {
                var today = DateTime.Today;
                Thang = today.Month;
                Nam = today.Year;
            }

            DoanhThuTheoNgay = await _context.HoaDon
                .Where(x => x.NgayHoaDon.Month == Thang && x.NgayHoaDon.Year == Nam)
                .GroupBy(d => d.NgayHoaDon.Date)
                .Select(g => new DoanhThuItem
                {
                    Ngay = g.Key,
                    SoDon = g.Where(x => x.IdNhomHoaDon < 10).Count(),
                    TaiCho = g.Where(x => x.IdNhomHoaDon == 1).Sum(x => x.TongTien),
                    MuaVe = g.Where(x => x.IdNhomHoaDon == 2).Sum(x => x.TongTien),
                    DiShip = g.Where(x => x.IdNhomHoaDon == 3).Sum(x => x.TongTien),
                    AppShipping = g.Where(x => x.IdNhomHoaDon == 4).Sum(x => x.TongTien),
                    TongTien = g.Where(x => x.IdNhomHoaDon < 10).Sum(x => x.TongTien),
                    ChiTieu = g.Where(x => x.IdNhomHoaDon > 10 && x.IdNhomHoaDon < 20).Sum(x => x.TongTien),
                    TienBank = g.Where(x => x.IdNhomHoaDon < 10).Sum(x => x.TienBank),
                    TienNo = g.Where(x => x.IdNhomHoaDon < 10 && x.NgayTra == null).Sum(x => x.TienNo),
                })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            TongDoanhThu = DoanhThuTheoNgay.Sum(d => d.TongTien);
            TongChiTieu = DoanhThuTheoNgay.Sum(d => d.ChiTieu);
            TongSoDon = DoanhThuTheoNgay.Sum(d => d.SoDon);
            TongChuyenKhoan = DoanhThuTheoNgay.Sum(d => d.TienBank);
            TongTienNo = DoanhThuTheoNgay.Sum(d => d.TienNo);
            TongTienMat = TongDoanhThu - TongChiTieu - TongChuyenKhoan - TongTienNo;
        }
        public class DoanhThuItem
        {
            public DateTime Ngay { get; set; }
            public int SoDon { get; set; }
            public double TongTien { get; set; }
            public double ChiTieu { get; set; }
            public double TienBank { get; set; }
            public double TienNo { get; set; }
            public double TaiCho { get; set; }
            public double MuaVe { get; set; }
            public double DiShip { get; set; }
            public double AppShipping { get; set; }


        }
    }
}