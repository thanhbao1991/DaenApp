using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Logging;
using TraSuaAppWeb.Data;
using TraSuaAppWeb.Hubs;
using TraSuaAppWeb.Models;

namespace TraSuaAppWeb.Pages
{
    public class ChiTietHoaDonModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<HoaDonHub> _hubContext;

        public ChiTietHoaDonModel(AppDbContext context
        , IHubContext<HoaDonHub> hubContext)

        {
            _context = context;
            _hubContext = hubContext;

        }
        public List<ChiTietHoaDon> ChiTietHoaDonList { get; set; } = new();
        public int IdHoaDon { get; set; }
        public int flag { get; set; }


        public async Task<IActionResult> OnGetAsync(int IdHoaDon, int flag)
        {
            try
            {
                this.IdHoaDon = IdHoaDon;
                this.flag = flag;


                ChiTietHoaDonList = await _context.ChiTietHoaDon
                    .AsNoTracking()
                    .Where(x => x.IdHoaDon == IdHoaDon)
                    .ToListAsync();

                return new PartialViewResult
                {
                    ViewName = "ChiTietHoaDonView",
                    ViewData = new ViewDataDictionary<ChiTietHoaDonModel>(
                        ViewData,
                        this
                    )
                };
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex);

                return new ContentResult
                {
                    Content = "<div class='alert alert-danger'>Không thể tải chi tiết hoá đơn.</div>",
                    ContentType = "text/html"
                };
            }
        }

        public async Task<IActionResult> OnPostCapNhatBankingAsync(int id)
        {
            try
            {
                var hoaDon = await _context.HoaDon.FindAsync(id);
                if (hoaDon == null)
                    return NotFound();

                // Tính phần cần banking lần này
                var tienCanBank = hoaDon.ConLai;
                if (tienCanBank <= 0)
                    return RedirectToPage("DoanhThuNgay"); // Không cần làm gì thêm

                // Cập nhật
                hoaDon.NgayBank = DateTime.Now;
                hoaDon.TienBank += tienCanBank;
                hoaDon.DaThu += tienCanBank;
                hoaDon.ConLai = hoaDon.TongTien - hoaDon.DaThu;

                // Nếu có nợ trước đó và giờ đã trả hết thì ghi ngày trả nợ
                if (hoaDon.NgayNo != null && hoaDon.ConLai == 0)
                    hoaDon.NgayTra = DateTime.Now; ;

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("BankingMoi", new
                {
                    id = hoaDon.IdHoaDon,
                    ten_khach = hoaDon.ThongTinHoaDon,
                    tong_tien = hoaDon.TienBank
                });

                return RedirectToPage("DoanhThuNgay");
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex);
                return new JsonResult(new { success = false });
            }
        }
        public async Task<IActionResult> OnPostCapNhatNoAsync(int id)
        {
            try
            {
                var hoaDon = await _context.HoaDon.FindAsync(id);
                if (hoaDon == null)
                    return NotFound();

                // Ghi nợ phần còn lại
                hoaDon.TienNo = hoaDon.ConLai;
                hoaDon.NgayNo = DateTime.Now;

                // Giữ nguyên phần đã thu
                // Không gán DaThu = 0

                hoaDon.DaThanhToan = true;
                hoaDon.NgayRa = DateTime.Now;

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("GhiNoMoi", new
                {
                    id = hoaDon.IdHoaDon,
                    ten_khach = hoaDon.ThongTinHoaDon,
                    tong_tien = hoaDon.TienNo
                });

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
