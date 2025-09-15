using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Models;

namespace TraSuaAppWeb.Pages
{
    public class DanhSachHoaDonModel : PageModel
    {
        private readonly AppDbContext _context;
        public DanhSachHoaDonModel(AppDbContext context)
        {
            _context = context;
        }
        public double TongTien = 0;
        public int Flag = 0;

        public List<HoaDon> HoaDonList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int flag, int Ngay, int Thang, int Nam)
        {
            Flag = flag;
            var currentDate = new DateTime(Nam, Thang, Ngay);

            DateTime startOfDay = currentDate.Date;
            DateTime endOfDay = currentDate.Date.AddDays(1);
            IQueryable<HoaDon> query = _context.HoaDon.AsNoTracking();

            switch (flag)
            {
                // Tong cac bill no
                case 0:
                    query = query.Where(x =>
                    x.ConLai > 0 && x.NgayNo != null);
                    break;
                // Banking trong ngayf
                case 1:
                    query = query
                    .Where(x => x.NgayHoaDon >= startOfDay && x.NgayHoaDon < endOfDay)
                    .Where(x => x.NgayBank >= startOfDay && x.NgayBank < endOfDay)
                    .Where(x => x.TienBank > 0);
                    break;
                // Bill No Trong ngayày
                case 2:
                    query = query
                    .Where(x => x.NgayHoaDon >= startOfDay && x.NgayHoaDon < endOfDay)
                    .Where(x => x.NgayNo >= startOfDay && x.NgayNo < endOfDay)
                    .Where(x => !(x.NgayTra >= startOfDay && x.NgayTra < endOfDay))
                    .Where(x => x.TienNo > 0);
                    break;
                // Tra No Bank
                case 3:
                    query = query.Where(x => x.NgayHoaDon < startOfDay
                                            && x.NgayTra.HasValue
                                            && x.NgayTra.Value >= startOfDay
                                            && x.NgayTra.Value < endOfDay
                                            && x.TienBank > 0);
                    break;
                // Tra No Tien
                case 4:
                    query = query.Where(x => x.NgayHoaDon < startOfDay
                                            && x.NgayTra.HasValue
                                            && x.NgayTra.Value >= startOfDay
                                            && x.NgayTra.Value < endOfDay
                                            && x.TienBank == 0);
                    break;

                default:
                    query = query.Where(x => x.IdKhachHang == flag && x.ConLai > 0);
                    break;
            }
            HoaDonList = await query.OrderBy(x => x.NgayHoaDon).ToListAsync();
            if (flag == 0)
                TongTien = HoaDonList.Sum(x => x.TienNo);
            else if (flag == 1 || flag == 3)
                TongTien = HoaDonList.Sum(x => x.TienBank);
            else if (flag == 2 || flag == 4)
                TongTien = HoaDonList.Sum(x => x.TienNo);
            else
                TongTien = HoaDonList.Sum(x => x.ConLai);

            return new PartialViewResult
            {
                ViewName = "DanhSachHoaDonView",
                ViewData = new ViewDataDictionary<DanhSachHoaDonModel>(
                    ViewData,
                    this // truyền toàn bộ model hiện tại
                )
            };
        }

        public async Task<IActionResult> OnPostCapNhatBankingNhieuAsync([FromBody] List<int> ids)
        {
            try
            {
                foreach (var id in ids)
                {
                    var hoaDon = await _context.HoaDon.FindAsync(id);
                    if (hoaDon == null) continue;

                    var tienBank = hoaDon.ConLai;
                    if (tienBank <= 0) continue;

                    hoaDon.NgayBank = DateTime.Now;
                    hoaDon.TienBank += tienBank;
                    hoaDon.DaThu += tienBank;
                    hoaDon.ConLai = hoaDon.TongTien - hoaDon.DaThu;

                    if (hoaDon.NgayNo != null && hoaDon.ConLai == 0)
                        hoaDon.NgayTra = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("DoanhThuNgay");
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex);
                return new JsonResult(new { success = false });
            }
        }
    }
}
