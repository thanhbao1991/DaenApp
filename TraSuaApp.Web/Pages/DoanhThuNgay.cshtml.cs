using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Models;


namespace TraSuaAppWeb.Pages
{
    public class DoanhThuNgayModel : PageModel
    {
        public List<HoaDon> HoaDonList { get; set; } = new();
        public double TongDoanhThu { get; set; }
        public double TongDaThu { get; set; }
        public double TongConLai { get; set; }
        public double TongChiTieu { get; set; }
        public double TongSoDon { get; set; }
        public double TongChuyenKhoan { get; set; }
        public double TongTienMat { get; set; }
        public double TongTienNo { get; set; }
        public double MuaVe { get; set; }
        public double TaiCho { get; set; }
        public double DiShip { get; set; }
        public double AppShipping { get; set; }
        public int id_ChiTieu { get; set; }
        public int id_viec_lam { get; set; }


        [BindProperty(SupportsGet = true)]
        public int Ngay { get; set; }
        [BindProperty(SupportsGet = true)]
        public int Thang { get; set; }
        [BindProperty(SupportsGet = true)]
        public int Nam { get; set; }

        private readonly AppDbContext _context;
        public DoanhThuNgayModel(AppDbContext context)
        {
            _context = context;
        }
        public async Task OnGetAsync()
        {
            if (Thang == 0 || Nam == 0 || Ngay == 0)
            {
                var today = DateTime.Today;
                Ngay = today.Day;
                Thang = today.Month;
                Nam = today.Year;
            }

            var currentDate = new DateTime(Nam, Thang, Ngay);
            try
            {
                HoaDonList = await _context.HoaDon.AsNoTracking()
                    .Where(x => x.NgayHoaDon >= currentDate && x.NgayHoaDon < currentDate.AddDays(1))
                    .OrderBy(x => x.DaThanhToan)
                    .ThenByDescending(x => x.NgayHoaDon)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                ViewData["SqlError"] = ex.ToString();
            }

            TongSoDon = HoaDonList.Where(x => x.IdNhomHoaDon < 10).Count();
            TongDoanhThu = HoaDonList.Where(x => x.IdNhomHoaDon < 10).Sum(d => d.TongTien);
            TongDaThu = HoaDonList.Where(x => x.IdNhomHoaDon < 10).Sum(d => d.DaThu);
            TongConLai = HoaDonList.Where(x => x.IdNhomHoaDon < 10).Sum(d => d.ConLai);

            TongChiTieu = HoaDonList.Where(x => x.IdNhomHoaDon > 10 && x.IdNhomHoaDon < 20).Sum(x => x.TongTien);
            TongChuyenKhoan = HoaDonList
                .Where(x => x.NgayBank >= currentDate && x.NgayBank < currentDate.AddDays(1))
                .Sum(d => d.TienBank);
            TongTienNo = HoaDonList
                .Where(x => x.NgayNo >= currentDate && x.NgayNo < currentDate.AddDays(1))
                .Where(x => !(x.NgayTra >= currentDate && x.NgayTra < currentDate.AddDays(1)))
                .Sum(d => d.TienNo);
            TongTienMat = TongDoanhThu - TongChiTieu - TongChuyenKhoan - TongTienNo;

            TaiCho = HoaDonList.Where(x => x.IdNhomHoaDon == 1).Sum(x => x.TongTien);
            MuaVe = HoaDonList.Where(x => x.IdNhomHoaDon == 2).Sum(x => x.TongTien);
            DiShip = HoaDonList.Where(x => x.IdNhomHoaDon == 3).Sum(x => x.TongTien);
            AppShipping = HoaDonList.Where(x => x.IdNhomHoaDon == 4).Sum(x => x.TongTien);

            id_ChiTieu = HoaDonList.FirstOrDefault(x => x.IdNhomHoaDon == 11)?.IdHoaDon ?? 0;
            id_viec_lam = HoaDonList.FirstOrDefault(x => x.IdNhomHoaDon == 21)?.IdHoaDon ?? 0;
        }
    }
}