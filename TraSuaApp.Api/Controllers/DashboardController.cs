using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;

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


            var topSanPham = await _db.ChiTietHoaDons
      .Where(c => c.HoaDon.Ngay >= today && c.HoaDon.Ngay < tomorrow && !c.HoaDon.IsDeleted)
      .GroupBy(c => c.TenSanPham)
      .Select(g => new DashboardTopSanPhamDto
      {
          TenSanPham = g.Key ?? "",
          SoLuong = g.Sum(x => x.SoLuong),
          DoanhThu = g.Sum(x => x.ThanhTien)
      })
      .OrderByDescending(x => x.DoanhThu)
      .ToListAsync();

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
            var prediction = await GPTService.DuDoanGioDongKhachAsync(detailedHeatmap);
            return new DashboardDto
            {
                PredictedPeak = prediction// ✅ Thêm dòng này
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