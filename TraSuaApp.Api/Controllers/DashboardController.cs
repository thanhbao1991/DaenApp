using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("homnay")]
        public async Task<ActionResult<DashboardDto>> GetToday()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var chiTietQuery = _db.ChiTietHoaDons
                .Where(c => c.HoaDon.Ngay >= today && c.HoaDon.Ngay < tomorrow && !c.HoaDon.IsDeleted);

            var tongDoanhThu = await chiTietQuery.SumAsync(x => x.ThanhTien);

            var tempList = await chiTietQuery
                .GroupBy(c => new { c.TenSanPham, Ngay = c.HoaDon.Ngay.Date })
                .Select(g => new
                {
                    Ngay = g.Key.Ngay,
                    TenSanPham = g.Key.TenSanPham ?? "",
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien),
                    TyLeDoanhThu = tongDoanhThu == 0
                        ? "0%"
                        : (Math.Round((g.Sum(x => x.ThanhTien) / tongDoanhThu) * 100, 2).ToString("0.00") + "%")
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToListAsync();

            // Gán STT sau khi có danh sách
            var topSanPham = tempList
                .Select((x, index) => new DashboardTopSanPhamDto
                {
                    Stt = index + 1,
                    Ngay = x.Ngay,
                    TenSanPham = x.TenSanPham,
                    SoLuong = x.SoLuong,
                    DoanhThu = x.DoanhThu,
                    TyLeDoanhThu = x.TyLeDoanhThu
                })
                .ToList();

            return new DashboardDto
            {
                TopSanPhams = topSanPham,
            };
        }

        [HttpGet("dubao")]
        public async Task<ActionResult<DashboardDto>> GetDuBaoToday()
        {
            var recentStart = DateTime.Today.AddMonths(-2);
            var recentEnd = DateTime.Today;

            var detailedHeatmap = await _db.HoaDons
                .Where(x => x.Ngay >= recentStart && x.Ngay < recentEnd && !x.IsDeleted)
                .GroupBy(x => new
                {
                    Date = x.NgayGio.Date,
                    Hour = x.NgayGio.Hour,
                    Minute = (x.NgayGio.Minute / 10) * 10
                })
                .Select(g => new TimeStat
                {
                    Date = g.Key.Date,
                    Hour = g.Key.Hour,
                    Minute = g.Key.Minute,
                    SoDon = g.Count(),
                    DoanhThu = g.Sum(x => x.ThanhTien),
                    Thu = "" // Tạm để trống, lát xử lý tiếp ở ngoài
                })
                .ToListAsync();

            // Sau khi lấy dữ liệu gọn, mới tính "Thứ" ở ngoài (không ảnh hưởng hiệu năng nhiều)
            foreach (var item in detailedHeatmap)
            {
                item.Thu = GetThuVietnamese(item.Date.DayOfWeek);
            }

            // Gọi GPT
            var prediction = await GeminiService.DuDoanGioDongKhachAsync(detailedHeatmap);
            return new DashboardDto
            {
                PredictedPeak = prediction// ✅ Thêm dòng này
            };
        }

        [HttpGet("lichsu-khachhang/{khachHangId}")]
        public async Task<ActionResult<DashboardDto>> GetLichSuKhachHang(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var history = await (
      from ct in _db.ChiTietHoaDons.AsNoTracking()
      join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
      where h.KhachHangId == khachHangId
          && !h.IsDeleted
      //&& ((ct.NoteText ?? "") != "" || (ct.ToppingText ?? "") != "")
      orderby h.NgayGio descending, ct.CreatedAt descending
      select new ChiTietHoaDonDto
      {
          Id = ct.Id,
          SoLuong = ct.SoLuong,
          DonGia = ct.DonGia,
          //ThanhTien = ct.ThanhTien,
          SanPhamIdBienThe = ct.SanPhamBienTheId,
          HoaDonId = ct.HoaDonId,
          NoteText = ct.NoteText,
          ToppingText = ct.ToppingText,
          CreatedAt = ct.CreatedAt,
          DeletedAt = ct.DeletedAt,
          IsDeleted = ct.IsDeleted,
          LastModified = ct.LastModified,
          TenBienThe = ct.TenBienThe,
          TenSanPham = ct.TenSanPham,
          NgayGio = h.NgayGio
      }
  ).ToListAsync();

            return new DashboardDto
            {
                History = history
            };
        }

        [HttpGet("thongtin-khachhang/{khachHangId}")]
        public async Task<ActionResult<KhachHangFavoriteDto>> GetThongTinKhachHang(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var kh = await _db.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == khachHangId);

            if (kh == null) return NotFound("Không tìm thấy khách hàng.");


            // 🟟 Top chi tiết
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            var topChiTiets = await (
                from ct in _db.ChiTietHoaDons.AsNoTracking()
                join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
                join bt in _db.SanPhamBienThes.AsNoTracking() on ct.SanPhamBienTheId equals bt.Id
                join sp in _db.SanPhams.AsNoTracking() on bt.SanPhamId equals sp.Id
                where h.KhachHangId == khachHangId
                      && !h.IsDeleted
                      && !ct.IsDeleted
                      && h.NgayGio >= threeMonthsAgo   // 🟟 chỉ lấy đơn trong 3 tháng gần đây
                group ct by new { bt.Id, sp.Ten, bt.TenBienThe, bt.GiaBan } into g
                orderby g.Sum(x => x.SoLuong) descending
                select new ChiTietHoaDonDto
                {
                    SanPhamIdBienThe = g.Key.Id,
                    TenSanPham = g.Key.Ten ?? "",        // 🟟 lấy tên mới nhất từ SanPhams
                    TenBienThe = g.Key.TenBienThe ?? "", // 🟟 lấy tên mới nhất từ SanPhamBienThes
                    DonGia = g.Key.GiaBan,           // 🟟 giá hiện tại của biến thể
                    SoLuong = 0
                }
            )
            .Take(3) // 🟟 chỉ lấy tối đa 2 món
            .ToListAsync();


            // 🟟 Tính điểm thưởng
            int diemThangNay = 0, diemThangTruoc = 0;
            if (kh.DuocNhanVoucher)
            {
                var now = DateTime.Now;
                var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
                diemThangNay = await _db.ChiTietHoaDonPoints.AsNoTracking()
                    .Where(p => p.KhachHangId == khachHangId && p.Ngay >= firstDayCurrent && p.Ngay <= now.Date)
                    .SumAsync(p => (int?)p.DiemThayDoi) ?? 0;

                var firstDayPrev = firstDayCurrent.AddMonths(-1);
                var lastDayPrev = firstDayCurrent.AddDays(-1);
                diemThangTruoc = await _db.ChiTietHoaDonPoints.AsNoTracking()
                    .Where(p => p.KhachHangId == khachHangId && p.Ngay >= firstDayPrev && p.Ngay <= lastDayPrev)
                    .SumAsync(p => (int?)p.DiemThayDoi) ?? 0;
            }
            else
            {
                diemThangNay = diemThangTruoc = -1;
            }

            // 🟟 Tính tổng nợ
            var congNoQuery = _db.ChiTietHoaDonNos.AsNoTracking()
                .Where(h => h.KhachHangId == khachHangId && !h.IsDeleted);

            var resultNo = await congNoQuery
                .Select(h => new
                {
                    ConLai = h.SoTienNo - (_db.ChiTietHoaDonThanhToans
                                            .Where(t => t.ChiTietHoaDonNoId == h.Id && !t.IsDeleted)
                                            .Sum(t => (decimal?)t.SoTien) ?? 0)
                })
                .ToListAsync();

            var tongNo = resultNo.Sum(x => x.ConLai > 0 ? x.ConLai : 0);

            /// 🟟 Kiểm tra khách hàng đã nhận voucher trong tháng này chưa
            bool daNhanVoucher = false;
            if (kh.DuocNhanVoucher)
            {
                var now = DateTime.Now;
                var firstDayCurrent = new DateTime(now.Year, now.Month, 1);

                daNhanVoucher = await (
                    from v in _db.ChiTietHoaDonVouchers.AsNoTracking()
                    join h in _db.HoaDons.AsNoTracking() on v.HoaDonId equals h.Id
                    where h.KhachHangId == khachHangId
                          && !h.IsDeleted
                          && !v.IsDeleted
                          && v.CreatedAt >= firstDayCurrent
                          && v.CreatedAt <= now
                    select v
                ).AnyAsync();
            }

            // 🟟 Trả về DTO gộp
            return new KhachHangFavoriteDto
            {
                KhachHangId = kh.Id,
                DuocNhanVoucher = kh.DuocNhanVoucher,
                DaNhanVoucher = daNhanVoucher, // ✅ đã sửa lại đúng cách
                DiemThangNay = diemThangNay,
                DiemThangTruoc = diemThangTruoc,
                TongNo = tongNo,
                TopChiTiets = topChiTiets
            };
        }

        private string GetThuVietnamese(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }
    }
}